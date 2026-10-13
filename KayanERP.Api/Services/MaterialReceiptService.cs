using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using KayanERP.Api.Models;
using KayanERP.Api.DTOs;

namespace KayanERP.Api.Services
{
    public interface IMaterialReceiptService
    {
        Task<MaterialReceiptLookupsDto> GetLookupsAsync();
        Task<List<PendingInvoiceDto>> GetPendingInvoicesAsync(long? supplierId = null);
        Task<MaterialReceiptResultDto> ProcessMaterialReceiptAsync(MaterialReceiptPostDto dto);
    }

    public class MaterialReceiptService : IMaterialReceiptService
    {
        private readonly AppDbContext _context;

        public MaterialReceiptService(AppDbContext context)
        {
            _context = context;
        }

        // ==============================================================
        // 1. القوائم المرجعية: حصر مستودعات الخامات المركزية وربطها بالفروع
        // ==============================================================
        public async Task<MaterialReceiptLookupsDto> GetLookupsAsync()
        {
            var suppliers = await _context.Suppliers
                .AsNoTracking()
                .Where(s => s.Active)
                .OrderBy(s => s.SupplierName)
                .Select(s => new MaterialReceiptSupplierLookupDto
                {
                    Id = s.Id,
                    SupplierName = s.SupplierName
                })
                .ToListAsync();

            var warehouses = await _context.Warehouses
                .Include(w => w.Branch)
                .AsNoTracking()
                .Where(w => w.Active &&
                           (w.WarehouseName.Contains("خامات")
                         || w.WarehouseName.Contains("رئيسي")
                         || w.WarehouseName.Contains("تعبئة")
                         || w.WarehouseName.Contains("مستودع")
                         || w.WarehouseName.Contains("مركزي"))
                         && !w.WarehouseName.Contains("صالة")
                         && !w.WarehouseName.Contains("قسم")
                         && !w.WarehouseName.Contains("إدارة"))
                .OrderBy(w => w.WarehouseName)
                .Select(w => new MaterialReceiptWarehouseLookupDto
                {
                    Id = w.Id,
                    WarehouseName = w.Branch != null
                        ? $"{w.WarehouseName} - [{w.Branch.BranchName}]"
                        : w.WarehouseName
                })
                .ToListAsync();

            if (!warehouses.Any())
            {
                warehouses = await _context.Warehouses
                    .Include(w => w.Branch)
                    .AsNoTracking()
                    .Where(w => w.Active && !w.WarehouseName.Contains("صالة") && !w.WarehouseName.Contains("قسم"))
                    .Select(w => new MaterialReceiptWarehouseLookupDto
                    {
                        Id = w.Id,
                        WarehouseName = w.Branch != null
                            ? $"{w.WarehouseName} - [{w.Branch.BranchName}]"
                            : w.WarehouseName
                    })
                    .ToListAsync();
            }

            return new MaterialReceiptLookupsDto
            {
                Suppliers = suppliers,
                Warehouses = warehouses
            };
        }

        // ==============================================================
        // 2. جلب الفواتير المعتمدة المفتوحة والمتبقي منها خامات للتوريد
        // ==============================================================
        public async Task<List<PendingInvoiceDto>> GetPendingInvoicesAsync(long? supplierId = null)
        {
            var query = _context.PurchaseOrders
                .AsNoTracking()
                .Include(p => p.Supplier)
                .Include(p => p.PurchaseOrderLines).ThenInclude(l => l.Item)
                .Where(p => p.Status == "Approved" || p.Status == "Partially_Received");

            if (supplierId.HasValue && supplierId.Value > 0)
                query = query.Where(p => p.SupplierId == supplierId.Value);

            query = query.Where(p => p.PurchaseOrderLines.Any(l => (l.Quantity - l.ReceivedQuantity) > 0));

            return await query
                .OrderByDescending(p => p.Id)
                .Select(p => new PendingInvoiceDto
                {
                    Id = p.Id,
                    OrderNumber = p.OrderNumber,
                    OrderDate = p.OrderDate.ToString("yyyy-MM-dd"),
                    SupplierId = p.SupplierId,
                    SupplierName = p.Supplier != null ? p.Supplier.SupplierName : "غير محدد",
                    WarehouseId = p.WarehouseId,
                    TotalAmount = p.TotalAmount,
                    Status = p.Status,
                    Lines = p.PurchaseOrderLines
                        .Where(l => (l.Quantity - l.ReceivedQuantity) > 0)
                        .Select(l => new PendingInvoiceLineDto
                        {
                            Id = l.Id,
                            ItemId = l.ItemId,
                            ItemCode = l.Item != null ? l.Item.ItemCode : string.Empty,
                            ItemName = l.Item != null ? l.Item.ItemName : string.Empty,
                            Unit = l.Item != null ? (l.Item.Unit ?? "كجم") : "كجم",
                            Quantity = l.Quantity,
                            ReceivedQuantity = l.ReceivedQuantity,
                            RemainingQuantity = l.Quantity - l.ReceivedQuantity,
                            UnitPrice = l.UnitPrice,
                            Notes = l.Notes
                        }).ToList()
                })
                .ToListAsync();
        }

        // ==============================================================
        // 3. تأكيد الاستلام الميداني وفحص الجودة والترصيد الذري
        // ==============================================================
        public async Task<MaterialReceiptResultDto> ProcessMaterialReceiptAsync(MaterialReceiptPostDto dto)
        {
            if (dto == null || dto.Items == null || !dto.Items.Any())
                throw new ArgumentException("لا توجد بنود للاستلام أو الفحص.");

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var order = await _context.PurchaseOrders
                    .Include(p => p.PurchaseOrderLines)
                    .FirstOrDefaultAsync(p => p.Id == dto.InvoiceId);

                if (order == null)
                    throw new InvalidOperationException("أمر الشراء / الفاتورة غير موجود.");

                if (order.Status == "Closed" || order.Status == "Received" || order.Status == "Rejected")
                    throw new InvalidOperationException("هذه الفاتورة مغلقة أو مرفوضة مسبقاً.");

                var targetWarehouseId = dto.WarehouseId > 0 ? dto.WarehouseId : order.WarehouseId;
                var now = DateTime.UtcNow;

                decimal totalAcceptedInReceipt = dto.Items.Sum(x => x.AcceptedQuantity);
                decimal totalRejectedInReceipt = dto.Items.Sum(x => x.RejectedQuantity);

                foreach (var item in dto.Items)
                {
                    var poLine = order.PurchaseOrderLines.FirstOrDefault(l => l.ItemId == item.ItemId);
                    if (poLine != null)
                    {
                        poLine.ReceivedQuantity += item.AcceptedQuantity;
                    }

                    // ترحيل الرصيد المقبول إلى مستودع الخامات
                    if (item.AcceptedQuantity > 0)
                    {
                        var balance = await _context.StockBalances
                            .FirstOrDefaultAsync(b => b.WarehouseId == targetWarehouseId && b.ItemId == item.ItemId);

                        if (balance != null)
                        {
                            balance.Quantity += item.AcceptedQuantity;
                            balance.AvailableQuantity = balance.Quantity - balance.ReservedQuantity;
                            balance.LastUpdatedAt = now;
                        }
                        else
                        {
                            _context.StockBalances.Add(new StockBalance
                            {
                                CompanyId = order.CompanyId,
                                WarehouseId = targetWarehouseId,
                                ItemId = item.ItemId,
                                Quantity = item.AcceptedQuantity,
                                AvailableQuantity = item.AcceptedQuantity,
                                ReservedQuantity = 0,
                                LastUpdatedAt = now
                            });
                        }

                        // تسجيل حركة الوارد في كارت الصنف مع بيانات الـ GMP
                        var qcNote = $"LOT: {item.BatchNumber} | صلاحية: {item.ExpiryDate:yyyy-MM-dd} | قرار: مقبول | المفتش: [{dto.InspectorCode}] {dto.InspectorName} | شهادة التحليل COA: {dto.CoaNumber}";
                        if (!string.IsNullOrWhiteSpace(item.RejectionReason) && item.RejectionReason != "مطابق للمواصفة")
                            qcNote += $" | ملاحظات: {item.RejectionReason}";

                        _context.StockMovements.Add(new StockMovement
                        {
                            CompanyId = order.CompanyId,
                            WarehouseId = targetWarehouseId,
                            ItemId = item.ItemId,
                            MovementType = "PURCHASE_RECEIPT",
                            Quantity = item.AcceptedQuantity,
                            UnitCost = item.UnitPrice,
                            ReferenceType = "PURCHASE_ORDER",
                            ReferenceId = order.Id,
                            Notes = qcNote,
                            MovementAt = now,
                            CreatedAt = now
                        });
                    }

                    // توثيق الكميات المرفوضة في سجل رقابة الجودة
                    if (item.RejectedQuantity > 0)
                    {
                        var rejectNote = $"LOT: {item.BatchNumber} | مرفوض فني | السبب: {item.RejectionReason} | المفتش: [{dto.InspectorCode}] {dto.InspectorName}";
                        _context.StockMovements.Add(new StockMovement
                        {
                            CompanyId = order.CompanyId,
                            WarehouseId = targetWarehouseId,
                            ItemId = item.ItemId,
                            MovementType = "QC_REJECTED",
                            Quantity = item.RejectedQuantity,
                            UnitCost = item.UnitPrice,
                            ReferenceType = "PURCHASE_ORDER",
                            ReferenceId = order.Id,
                            Notes = rejectNote,
                            MovementAt = now,
                            CreatedAt = now
                        });
                    }
                }

                // محرك تحديث حالة الفاتورة
                string finalMessage;

                if (totalAcceptedInReceipt == 0 && totalRejectedInReceipt > 0 && order.PurchaseOrderLines.All(l => l.ReceivedQuantity == 0))
                {
                    order.Status = "Rejected";
                    finalMessage = $"تم تسجيل رفض الشحنة بالكامل وإغلاق الفاتورة رقم [{order.OrderNumber}].";
                }
                else if (order.PurchaseOrderLines.All(l => l.ReceivedQuantity >= l.Quantity))
                {
                    order.Status = "Closed";
                    finalMessage = $"تم اعتماد إذن الفحص وترحيل الرصيد لمخزن الخامات وإغلاق الفاتورة رقم [{order.OrderNumber}] بنجاح!";
                }
                else
                {
                    order.Status = "Partially_Received";
                    finalMessage = $"تم إثبات الاستلام الجزئي للفاتورة رقم [{order.OrderNumber}] وتحديث الأرصدة المتبقية.";
                }

                order.UpdatedAt = now;

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return new MaterialReceiptResultDto
                {
                    Success = true,
                    NewStatus = order.Status,
                    Message = finalMessage
                };
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
    }
}