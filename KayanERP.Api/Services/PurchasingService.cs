using Microsoft.EntityFrameworkCore;
using KayanERP.Api.Models;
using KayanERP.Shared.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KayanERP.Api.Services
{
    public class PurchasingService : IPurchasingService
    {
        private readonly AppDbContext _context;

        public PurchasingService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<PurchaseOrderLookupDto>> GetApprovedPurchaseOrdersAsync()
        {
            var orders = await _context.PurchaseOrders
                .Where(p => p.Status == "Approved" || p.Status == "Partially_Received")
                .OrderByDescending(p => p.Id)
                .ToListAsync();

            var result = new List<PurchaseOrderLookupDto>();

            foreach (var ord in orders)
            {
                var supplierName = await _context.Suppliers
                    .Where(s => s.Id == ord.SupplierId)
                    .Select(s => s.SupplierName)
                    .FirstOrDefaultAsync() ?? "مورد غير محدد";

                var warehouseName = await _context.Warehouses
                    .Where(w => w.Id == ord.WarehouseId)
                    .Select(w => w.WarehouseName)
                    .FirstOrDefaultAsync() ?? "المخزن الرئيسي";

                var poLines = await _context.PurchaseOrderLines
                    .Where(l => l.PurchaseOrderId == ord.Id && l.Quantity > l.ReceivedQuantity)
                    .ToListAsync();

                if (poLines.Count == 0) continue;

                var itemIds = poLines.Select(l => l.ItemId).Distinct().ToList();
                var itemsMap = await _context.Items
                    .Where(i => itemIds.Contains(i.Id))
                    .ToDictionaryAsync(i => i.Id, i => new { i.ItemCode, i.ItemName, i.HasExpiryDate, i.ShelfLifeDays });

                var linesDto = poLines.Select(l => new PurchaseOrderLineDto
                {
                    ItemId = l.ItemId,
                    ItemCode = itemsMap.ContainsKey(l.ItemId) ? itemsMap[l.ItemId].ItemCode : "",
                    ItemName = itemsMap.ContainsKey(l.ItemId) ? itemsMap[l.ItemId].ItemName : "صنف غير معرف",
                    OrderedQuantity = l.Quantity - l.ReceivedQuantity,
                    UnitCost = l.UnitPrice,
                    HasExpiryDate = itemsMap.ContainsKey(l.ItemId) ? itemsMap[l.ItemId].HasExpiryDate : true,
                    ShelfLifeDays = itemsMap.ContainsKey(l.ItemId) ? itemsMap[l.ItemId].ShelfLifeDays : null
                }).ToList();

                result.Add(new PurchaseOrderLookupDto
                {
                    Id = ord.Id,
                    OrderNumber = ord.OrderNumber,
                    SupplierName = supplierName,
                    WarehouseId = ord.WarehouseId,
                    WarehouseName = warehouseName,
                    OrderDate = ord.OrderDate.ToString("yyyy-MM-dd"),
                    Lines = linesDto
                });
            }

            return result;
        }

        public async Task<bool> ReceiveGoodsAsync(MaterialReceiptDto dto)
        {
            if (dto == null || dto.Lines == null || dto.Lines.Count == 0)
                return false;

            var po = await _context.PurchaseOrders.FirstOrDefaultAsync(p => p.Id == dto.PurchaseOrderId);
            if (po == null) return false;

            var poLines = await _context.PurchaseOrderLines
                .Where(l => l.PurchaseOrderId == po.Id)
                .ToListAsync();

            DateTime executionTime = DateTime.UtcNow;

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                foreach (var line in dto.Lines)
                {
                    if (line.ReceivedQuantity <= 0) continue;

                    var poLine = poLines.FirstOrDefault(l => l.ItemId == line.ItemId);
                    decimal unitCost = poLine?.UnitPrice ?? 0;

                    // حساب الكميات بدقة بناءً على قرار الجودة
                    decimal finalAccepted = line.QcStatus == "Rejected" ? 0 :
                                            (line.QcStatus == "Partially_Accepted" ? line.AcceptedQuantity : line.ReceivedQuantity);

                    decimal finalRejected = line.QcStatus == "Accepted" ? 0 :
                                            (line.QcStatus == "Partially_Accepted" ? line.RejectedQuantity : line.ReceivedQuantity);

                    // بناء الملاحظات الرقابية المعتمدة
                    string qcNotes = $"[قرار الجودة: {line.QcStatus}] مفتش: {dto.QcInspectorName}";
                    if (!string.IsNullOrWhiteSpace(dto.CoaNumber)) qcNotes += $" | شهادة: {dto.CoaNumber}";
                    if (finalRejected > 0 && !string.IsNullOrWhiteSpace(line.RejectionReason)) qcNotes += $" | سبب الرفض: {line.RejectionReason}";

                    string expiryInfo = line.HasExpiryDate
                        ? $"تشغيلة: {line.BatchNumber} | صلاحية: {line.ExpiryDate:yyyy-MM-dd}"
                        : $"تشغيلة/تتبع: {line.BatchNumber}";

                    string movementNotes = $"{expiryInfo} | {qcNotes}";

                    // 1. تدوين حركة المخزن للكمية المقبولة فقط (التي ستدخل الرصيد المتاح)
                    if (finalAccepted > 0)
                    {
                        var acceptedMovement = new StockMovement
                        {
                            CompanyId = po.CompanyId,
                            WarehouseId = dto.WarehouseId,
                            ItemId = line.ItemId,
                            MovementType = "PURCHASE_RECEIPT",
                            Quantity = finalAccepted,
                            UnitCost = unitCost,
                            ReferenceType = "PURCHASE_ORDER",
                            ReferenceId = po.Id,
                            Notes = movementNotes,
                            MovementAt = DateTime.SpecifyKind(dto.ReceiptDate, DateTimeKind.Utc),
                            CreatedAt = executionTime
                        };
                        _context.StockMovements.Add(acceptedMovement);

                        // 2. تحديث الرصيد المخزني الفعلي بالكمية المقبولة فقط
                        var balance = await _context.StockBalances
                            .FirstOrDefaultAsync(b => b.WarehouseId == dto.WarehouseId && b.ItemId == line.ItemId && b.CompanyId == po.CompanyId);

                        if (balance == null)
                        {
                            balance = new StockBalance
                            {
                                CompanyId = po.CompanyId,
                                WarehouseId = dto.WarehouseId,
                                ItemId = line.ItemId,
                                Quantity = finalAccepted,
                                ReservedQuantity = 0,
                                AvailableQuantity = finalAccepted,
                                LastUpdatedAt = executionTime
                            };
                            _context.StockBalances.Add(balance);
                        }
                        else
                        {
                            balance.Quantity += finalAccepted;
                            balance.AvailableQuantity = balance.Quantity - balance.ReservedQuantity;
                            balance.LastUpdatedAt = executionTime;
                        }
                    }

                    // 3. تسجيل حركة رفض فني إن وُجدت كميات مرفوضة (لإثبات المرتجع وعدم إضافتها للرصيد)
                    if (finalRejected > 0)
                    {
                        var rejectedMovement = new StockMovement
                        {
                            CompanyId = po.CompanyId,
                            WarehouseId = dto.WarehouseId,
                            ItemId = line.ItemId,
                            MovementType = "QC_REJECTED",
                            Quantity = finalRejected,
                            UnitCost = unitCost,
                            ReferenceType = "PURCHASE_ORDER",
                            ReferenceId = po.Id,
                            Notes = $"[شحنة مرفوضة بالرصيف] سبب: {line.RejectionReason} | مفتش: {dto.QcInspectorName}",
                            MovementAt = DateTime.SpecifyKind(dto.ReceiptDate, DateTimeKind.Utc),
                            CreatedAt = executionTime
                        };
                        _context.StockMovements.Add(rejectedMovement);
                    }

                    // 4. تحديث كمية سطر أمر الشراء (الكمية المستلمة فعلياً والمعتمدة)
                    if (poLine != null)
                    {
                        poLine.ReceivedQuantity += finalAccepted;
                    }
                }

                bool allFullyReceived = poLines.All(l => l.ReceivedQuantity >= l.Quantity);
                po.Status = allFullyReceived ? "Received" : "Partially_Received";
                po.UpdatedAt = executionTime;

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return true;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<bool> CreateInvoiceAsync(PurchaseInvoiceCreateDto dto)
        {
            if (dto == null) return false;
            await Task.CompletedTask;
            return true;
        }

        public Task<List<PurchaseOrderLookupDto>> GetOrdersReadyForReceiptAsync() => GetApprovedPurchaseOrdersAsync();
        public Task<bool> ReceiveMaterialsAsync(MaterialReceiptDto dto) => ReceiveGoodsAsync(dto);
    }
}