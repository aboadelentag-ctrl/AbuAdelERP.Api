using Microsoft.EntityFrameworkCore;
using KayanERP.Api.Models;
using KayanERP.Shared.DTOs;

namespace KayanERP.Api.Services
{
    // الإنترفيس المعتمد
    public interface IProcurementService
    {
        Task<List<ProcurementPendingRequestDto>> GetPendingRequestsAsync();
        Task<ProcurementPendingRequestDto?> GetRequestDetailsAsync(long requestId);
        Task<bool> ConvertToApprovedInvoiceAsync(ProcurementConvertDto dto);
        Task<List<ProcurementApprovedInvoiceDto>> GetApprovedInvoicesAsync();
    }

    public class ProcurementService : IProcurementService
    {
        private readonly AppDbContext _context;

        public ProcurementService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<ProcurementPendingRequestDto>> GetPendingRequestsAsync()
        {
            return await _context.PurchaseOrders
                .Where(p => p.Status == "Pending_Procurement")
                .OrderByDescending(p => p.Id)
                .Select(p => new ProcurementPendingRequestDto
                {
                    Id = p.Id,
                    RequestNumber = p.OrderNumber,
                    WarehouseId = p.WarehouseId,
                    WarehouseName = _context.Warehouses.Where(w => w.Id == p.WarehouseId).Select(w => w.WarehouseName).FirstOrDefault() ?? "المخزن الرئيسي",
                    RequestDate = p.OrderDate.ToDateTime(TimeOnly.MinValue),
                    Status = p.Status,
                    ItemsCount = _context.PurchaseOrderLines.Count(l => l.PurchaseOrderId == p.Id)
                })
                .ToListAsync();
        }

        public async Task<ProcurementPendingRequestDto?> GetRequestDetailsAsync(long requestId)
        {
            var po = await _context.PurchaseOrders.FirstOrDefaultAsync(p => p.Id == requestId);
            if (po == null) return null;

            var rawLines = await _context.PurchaseOrderLines
                .Where(l => l.PurchaseOrderId == requestId)
                .ToListAsync();

            var itemIds = rawLines.Select(l => l.ItemId).Distinct().ToList();
            var itemsMap = await _context.Items
                .Where(i => itemIds.Contains(i.Id))
                .ToDictionaryAsync(i => i.Id, i => new { i.ItemCode, i.ItemName, i.Unit });

            var defaultSupplierId = po.SupplierId > 0 ? po.SupplierId : 0;

            var linesDto = rawLines.Select(l => new ProcurementPendingLineDto
            {
                LineId = l.Id,
                ItemId = l.ItemId,
                SupplierId = defaultSupplierId,
                ItemCode = itemsMap.ContainsKey(l.ItemId) ? itemsMap[l.ItemId].ItemCode : "",
                ItemName = itemsMap.ContainsKey(l.ItemId) ? itemsMap[l.ItemId].ItemName : "صنف غير معرف",
                Unit = itemsMap.ContainsKey(l.ItemId) ? itemsMap[l.ItemId].Unit : "",
                RequestedQuantity = l.Quantity,
                UnitPrice = l.UnitPrice > 0 ? l.UnitPrice : 1,
                DiscountAmount = l.DiscountAmount,
                TaxAmount = l.TaxAmount
            }).ToList();

            return new ProcurementPendingRequestDto
            {
                Id = po.Id,
                RequestNumber = po.OrderNumber,
                WarehouseId = po.WarehouseId,
                WarehouseName = _context.Warehouses.Where(w => w.Id == po.WarehouseId).Select(w => w.WarehouseName).FirstOrDefault() ?? "المخزن الرئيسي",
                RequestDate = po.OrderDate.ToDateTime(TimeOnly.MinValue),
                Status = po.Status,
                ItemsCount = linesDto.Count,
                Lines = linesDto
            };
        }

        public async Task<bool> ConvertToApprovedInvoiceAsync(ProcurementConvertDto dto)
        {
            if (dto == null || dto.RequestId <= 0 || dto.Lines == null || dto.Lines.Count == 0)
                return false;

            if (dto.Lines.Any(l => l.SupplierId <= 0))
                throw new Exception("يجب تحديد المورد لكل صنف من الأصناف قبل الاعتماد.");

            var originalPo = await _context.PurchaseOrders.FirstOrDefaultAsync(p => p.Id == dto.RequestId);
            if (originalPo == null) return false;

            var originalLines = await _context.PurchaseOrderLines
                .Where(l => l.PurchaseOrderId == dto.RequestId)
                .ToListAsync();

            var supplierGroups = dto.Lines.GroupBy(l => l.SupplierId).ToList();

            string baseNumber = string.IsNullOrWhiteSpace(dto.InvoiceNumberPrefix)
                ? originalPo.OrderNumber.Replace("PR-", "INV-")
                : dto.InvoiceNumberPrefix.Trim();

            int groupIndex = 1;
            DateTime executionTime = DateTime.UtcNow;

            foreach (var group in supplierGroups)
            {
                long supplierId = group.Key;
                decimal subtotal = 0;
                decimal totalDiscount = 0;
                decimal totalTax = 0;

                foreach (var line in group)
                {
                    subtotal += (line.Quantity * line.UnitPrice);
                    totalDiscount += line.DiscountAmount;
                    totalTax += line.TaxAmount;
                }

                decimal netTotal = subtotal - totalDiscount + totalTax;

                if (groupIndex == 1)
                {
                    originalPo.SupplierId = supplierId;
                    originalPo.OrderNumber = supplierGroups.Count > 1 ? $"{baseNumber}-A" : baseNumber;
                    originalPo.OrderDate = DateOnly.FromDateTime(dto.InvoiceDate);
                    originalPo.Subtotal = subtotal;
                    originalPo.DiscountAmount = totalDiscount;
                    originalPo.TaxAmount = totalTax;
                    originalPo.TotalAmount = netTotal;
                    originalPo.Status = "Approved";
                    originalPo.UpdatedAt = executionTime;

                    var currentGroupLineIds = group.Select(g => g.LineId).ToList();
                    var linesToKeep = originalLines.Where(l => currentGroupLineIds.Contains(l.Id)).ToList();
                    var linesToMove = originalLines.Where(l => !currentGroupLineIds.Contains(l.Id)).ToList();

                    foreach (var kLine in linesToKeep)
                    {
                        var input = group.First(g => g.LineId == kLine.Id);
                        kLine.Quantity = input.Quantity;
                        kLine.UnitPrice = input.UnitPrice;
                        kLine.DiscountAmount = input.DiscountAmount;
                        kLine.TaxAmount = input.TaxAmount;
                    }

                    _context.PurchaseOrderLines.RemoveRange(linesToMove);
                }
                else
                {
                    char suffix = (char)('A' + (groupIndex - 1));
                    var newPo = new PurchaseOrder
                    {
                        CompanyId = originalPo.CompanyId,
                        SupplierId = supplierId,
                        WarehouseId = originalPo.WarehouseId,
                        OrderNumber = $"{baseNumber}-{suffix}",
                        OrderDate = DateOnly.FromDateTime(dto.InvoiceDate),
                        Status = "Approved",
                        Subtotal = subtotal,
                        DiscountAmount = totalDiscount,
                        TaxAmount = totalTax,
                        TotalAmount = netTotal,
                        CreatedAt = executionTime,
                        UpdatedAt = executionTime
                    };

                    _context.PurchaseOrders.Add(newPo);
                    await _context.SaveChangesAsync();

                    foreach (var input in group)
                    {
                        var newLine = new PurchaseOrderLine
                        {
                            PurchaseOrderId = newPo.Id,
                            ItemId = input.ItemId,
                            Quantity = input.Quantity,
                            UnitPrice = input.UnitPrice,
                            DiscountAmount = input.DiscountAmount,
                            TaxAmount = input.TaxAmount,
                            ReceivedQuantity = 0
                        };
                        _context.PurchaseOrderLines.Add(newLine);
                    }
                }

                groupIndex++;
            }

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<List<ProcurementApprovedInvoiceDto>> GetApprovedInvoicesAsync()
        {
            return await _context.PurchaseOrders
                .Where(p => p.Status == "Approved")
                .OrderByDescending(p => p.Id)
                .Select(p => new ProcurementApprovedInvoiceDto
                {
                    Id = p.Id,
                    InvoiceNumber = p.OrderNumber,
                    SupplierName = _context.Suppliers.Where(s => s.Id == p.SupplierId).Select(s => s.SupplierName).FirstOrDefault() ?? "مورد غير محدد",
                    WarehouseName = _context.Warehouses.Where(w => w.Id == p.WarehouseId).Select(w => w.WarehouseName).FirstOrDefault() ?? "المخزن الرئيسي",
                    InvoiceDate = p.OrderDate.ToDateTime(TimeOnly.MinValue),
                    TotalAmount = p.TotalAmount,
                    Status = p.Status
                })
                .ToListAsync();
        }
    }
}