using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using KayanERP.Api.Models;
using KayanERP.Api.DTOs;

namespace KayanERP.Api.Services
{
    public interface IPurchaseOrderService
    {
        Task<List<PurchaseOrderDto>> GetAllOrdersAsync();
        Task<PurchaseOrderDto?> GetOrderByIdAsync(long id);
        Task<PurchaseOrderResultDto> CreateOrderAsync(PurchaseOrderCreateDto dto);
        Task<bool> ApproveInvoiceAsync(PoApproveDto dto);
        Task<PurchaseOrderLookupsDto> GetLookupsAsync();
    }

    public class PurchaseOrderService : IPurchaseOrderService
    {
        private readonly AppDbContext _context;

        public PurchaseOrderService(AppDbContext context)
        {
            _context = context;
        }

        // ==============================================================
        // 1. القوائم المرجعية الموحدة (موردين، مستودعات بالفروع، وخامات)
        // ==============================================================
        public async Task<PurchaseOrderLookupsDto> GetLookupsAsync()
        {
            var suppliers = await _context.Suppliers
                .AsNoTracking()
                .Where(s => s.Active)
                .OrderBy(s => s.SupplierName)
                .Select(s => new PoSupplierLookupDto
                {
                    Id = s.Id,
                    SupplierCode = s.SupplierCode,
                    SupplierName = s.SupplierName,
                    Phone = s.Phone,
                    PaymentTerms = s.PaymentTerms
                })
                .ToListAsync();

            var warehouses = await _context.Warehouses
                .Include(w => w.Branch)
                .AsNoTracking()
                .Where(w => w.Active)
                .OrderBy(w => w.WarehouseName)
                .Select(w => new PoWarehouseLookupDto
                {
                    Id = w.Id,
                    WarehouseCode = w.WarehouseCode,
                    WarehouseName = w.Branch != null
                        ? $"{w.WarehouseName} - [{w.Branch.BranchName}]"
                        : w.WarehouseName
                })
                .ToListAsync();

            var items = await _context.Items
                .Include(i => i.ItemCategory)
                .AsNoTracking()
                .Where(i => i.Active)
                .OrderBy(i => i.ItemName)
                .Select(i => new PoItemLookupDto
                {
                    Id = i.Id,
                    ItemCode = i.ItemCode,
                    ItemName = i.ItemName,
                    Unit = i.Unit ?? "وحدة",
                    BasePrice = i.BasePrice,
                    CategoryName = i.ItemCategory != null ? i.ItemCategory.CategoryName : "عام"
                })
                .ToListAsync();

            return new PurchaseOrderLookupsDto
            {
                Suppliers = suppliers,
                Warehouses = warehouses,
                Items = items
            };
        }

        // ==============================================================
        // 2. جلب كافة أوامر الشراء
        // ==============================================================
        public async Task<List<PurchaseOrderDto>> GetAllOrdersAsync()
        {
            return await _context.PurchaseOrders
                .AsNoTracking()
                .Include(po => po.Supplier)
                .Include(po => po.PurchaseOrderLines)
                    .ThenInclude(l => l.Item)
                .OrderByDescending(po => po.Id)
                .Select(po => new PurchaseOrderDto
                {
                    Id = po.Id,
                    CompanyId = po.CompanyId,
                    SupplierId = po.SupplierId,
                    SupplierName = po.Supplier != null ? po.Supplier.SupplierName : "قيد التحديد في الفاتورة",
                    WarehouseId = po.WarehouseId,
                    OrderNumber = po.OrderNumber,
                    OrderDate = po.OrderDate.ToDateTime(TimeOnly.MinValue),
                    Status = po.Status,
                    Subtotal = po.Subtotal,
                    DiscountAmount = po.DiscountAmount,
                    TaxAmount = po.TaxAmount,
                    TotalAmount = po.TotalAmount,
                    Lines = po.PurchaseOrderLines.Select(l => new PurchaseOrderDetailDto
                    {
                        Id = l.Id,
                        PurchaseOrderId = l.PurchaseOrderId,
                        ItemId = l.ItemId,
                        ItemCode = l.Item != null ? l.Item.ItemCode : string.Empty,
                        ItemName = l.Item != null ? l.Item.ItemName : string.Empty,
                        Unit = l.Item != null ? (l.Item.Unit ?? string.Empty) : string.Empty,
                        Quantity = l.Quantity,
                        UnitPrice = l.UnitPrice,
                        DiscountAmount = l.DiscountAmount,
                        TaxAmount = l.TaxAmount,
                        ReceivedQuantity = l.ReceivedQuantity,
                        Notes = l.Notes
                    }).ToList()
                })
                .ToListAsync();
        }

        // ==============================================================
        // 3. جلب أمر شراء محدد
        // ==============================================================
        public async Task<PurchaseOrderDto?> GetOrderByIdAsync(long id)
        {
            var po = await _context.PurchaseOrders
                .AsNoTracking()
                .Include(p => p.Supplier)
                .Include(p => p.PurchaseOrderLines)
                    .ThenInclude(l => l.Item)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (po == null) return null;

            return new PurchaseOrderDto
            {
                Id = po.Id,
                CompanyId = po.CompanyId,
                SupplierId = po.SupplierId,
                SupplierName = po.Supplier != null ? po.Supplier.SupplierName : "قيد التحديد في الفاتورة",
                WarehouseId = po.WarehouseId,
                OrderNumber = po.OrderNumber,
                OrderDate = po.OrderDate.ToDateTime(TimeOnly.MinValue),
                Status = po.Status,
                Subtotal = po.Subtotal,
                DiscountAmount = po.DiscountAmount,
                TaxAmount = po.TaxAmount,
                TotalAmount = po.TotalAmount,
                Lines = po.PurchaseOrderLines.Select(l => new PurchaseOrderDetailDto
                {
                    Id = l.Id,
                    PurchaseOrderId = l.PurchaseOrderId,
                    ItemId = l.ItemId,
                    ItemCode = l.Item != null ? l.Item.ItemCode : string.Empty,
                    ItemName = l.Item != null ? l.Item.ItemName : string.Empty,
                    Unit = l.Item != null ? (l.Item.Unit ?? string.Empty) : string.Empty,
                    Quantity = l.Quantity,
                    UnitPrice = l.UnitPrice,
                    DiscountAmount = l.DiscountAmount,
                    TaxAmount = l.TaxAmount,
                    ReceivedQuantity = l.ReceivedQuantity,
                    Notes = l.Notes
                }).ToList()
            };
        }

        // ==============================================================
        // 4. إصدار أمر شراء جديد
        // ==============================================================
        public async Task<PurchaseOrderResultDto> CreateOrderAsync(PurchaseOrderCreateDto dto)
        {
            if (dto == null || dto.Lines == null || !dto.Lines.Any())
                throw new ArgumentException("يجب إضافة بنود للطلب أولاً.");

            decimal subtotal = dto.Lines.Sum(l => l.Quantity * l.UnitPrice);
            decimal discount = dto.Lines.Sum(l => l.DiscountAmount);
            decimal tax = dto.Lines.Sum(l => l.TaxAmount);
            decimal total = subtotal - discount + tax;

            string generatedOrderNumber = !string.IsNullOrWhiteSpace(dto.OrderNumber)
                ? dto.OrderNumber.Trim()
                : $"PO-{DateTime.UtcNow:yyyyMMdd}-{new Random().Next(100, 999)}";

            long resolvedSupplierId = dto.SupplierId;
            bool supplierExists = await _context.Suppliers.AnyAsync(s => s.Id == resolvedSupplierId);
            if (!supplierExists)
            {
                resolvedSupplierId = await _context.Suppliers.Select(s => s.Id).FirstOrDefaultAsync();
            }

            long resolvedWarehouseId = dto.WarehouseId;
            bool warehouseExists = await _context.Warehouses.AnyAsync(w => w.Id == resolvedWarehouseId);
            if (!warehouseExists)
            {
                resolvedWarehouseId = await _context.Warehouses.Select(w => w.Id).FirstOrDefaultAsync();
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var po = new PurchaseOrder
                {
                    CompanyId = dto.CompanyId <= 0 ? 1 : dto.CompanyId,
                    SupplierId = resolvedSupplierId,
                    WarehouseId = resolvedWarehouseId,
                    OrderNumber = generatedOrderNumber,
                    OrderDate = DateOnly.FromDateTime(dto.OrderDate),
                    Status = string.IsNullOrWhiteSpace(dto.Status) ? "Pending_Procurement" : dto.Status,
                    Subtotal = subtotal,
                    DiscountAmount = discount,
                    TaxAmount = tax,
                    TotalAmount = total,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.PurchaseOrders.Add(po);
                await _context.SaveChangesAsync();

                foreach (var line in dto.Lines)
                {
                    var poLine = new PurchaseOrderLine
                    {
                        PurchaseOrderId = po.Id,
                        ItemId = line.ItemId,
                        Quantity = line.Quantity,
                        UnitPrice = line.UnitPrice,
                        DiscountAmount = line.DiscountAmount,
                        TaxAmount = line.TaxAmount,
                        ReceivedQuantity = 0,
                        LineTotal = (line.Quantity * line.UnitPrice) - line.DiscountAmount + line.TaxAmount,
                        Notes = line.Notes?.Trim()
                    };
                    _context.PurchaseOrderLines.Add(poLine);
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return new PurchaseOrderResultDto
                {
                    Success = true,
                    Id = po.Id,
                    OrderNumber = po.OrderNumber,
                    Message = "تم إنشاء أمر الشراء بنجاح."
                };
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        // ==============================================================
        // 5. اعتماد أمر الشراء والفاتورة وتحديث الأسعار (مُعدلة ومُحصنة)
        // ==============================================================
        public async Task<bool> ApproveInvoiceAsync(PoApproveDto dto)
        {
            if (dto == null || dto.OrderId <= 0 || dto.Lines == null || !dto.Lines.Any())
                throw new ArgumentException("بيانات غير صالحة للاعتماد.");

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var order = await _context.PurchaseOrders
                    .Include(p => p.PurchaseOrderLines)
                    .FirstOrDefaultAsync(p => p.Id == dto.OrderId);

                if (order == null) return false;

                order.Status = string.IsNullOrWhiteSpace(dto.Status) ? "Approved" : dto.Status;
                order.UpdatedAt = DateTime.UtcNow;

                decimal subtotal = 0;
                decimal totalDiscount = 0;
                decimal totalTax = 0;

                foreach (var editLine in dto.Lines)
                {
                    var existingLine = order.PurchaseOrderLines.FirstOrDefault(l => l.Id == editLine.LineId || l.ItemId == editLine.ItemId);
                    if (existingLine != null)
                    {
                        existingLine.UnitPrice = editLine.UnitPrice;
                        existingLine.Quantity = editLine.Quantity;
                        existingLine.LineTotal = (editLine.Quantity * editLine.UnitPrice) - existingLine.DiscountAmount + existingLine.TaxAmount;

                        if (!string.IsNullOrWhiteSpace(editLine.Notes))
                            existingLine.Notes = editLine.Notes;

                        subtotal += (editLine.Quantity * editLine.UnitPrice);
                        totalDiscount += existingLine.DiscountAmount;
                        totalTax += existingLine.TaxAmount;
                    }
                }

                // تحديث القيم المحاسبية بدقة
                order.Subtotal = subtotal;
                order.DiscountAmount = totalDiscount;
                order.TaxAmount = totalTax;
                order.TotalAmount = subtotal - totalDiscount + totalTax;

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
    }

    // ==============================================================
    // DTOs المرجعية لأوامر الشراء داخل نفس الـ Namespace
    // ==============================================================
    public class PurchaseOrderLookupsDto
    {
        public List<PoSupplierLookupDto> Suppliers { get; set; } = new();
        public List<PoWarehouseLookupDto> Warehouses { get; set; } = new();
        public List<PoItemLookupDto> Items { get; set; } = new();
    }

    public class PoSupplierLookupDto
    {
        public long Id { get; set; }
        public string SupplierCode { get; set; } = string.Empty;
        public string SupplierName { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string? PaymentTerms { get; set; }
    }

    public class PoWarehouseLookupDto
    {
        public long Id { get; set; }
        public string WarehouseCode { get; set; } = string.Empty;
        public string WarehouseName { get; set; } = string.Empty;
    }

    public class PoItemLookupDto
    {
        public long Id { get; set; }
        public string ItemCode { get; set; } = string.Empty;
        public string ItemName { get; set; } = string.Empty;
        public string Unit { get; set; } = string.Empty;
        public decimal BasePrice { get; set; }
        public string CategoryName { get; set; } = string.Empty;
    }
}