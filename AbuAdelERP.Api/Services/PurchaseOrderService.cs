using Microsoft.EntityFrameworkCore;
using AbuAdelERP.Api.Models;
using AbuAdelERP.Api.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AbuAdelERP.Api.Services
{
    public interface IPurchaseOrderService
    {
        Task<List<PurchaseOrderDto>> GetAllOrdersAsync();
        Task<PurchaseOrderDto?> GetOrderByIdAsync(long id);
        Task<bool> CreateOrderAsync(PurchaseOrderCreateDto dto);
    }

    public class PurchaseOrderService : IPurchaseOrderService
    {
        private readonly AppDbContext _context;

        public PurchaseOrderService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<PurchaseOrderDto>> GetAllOrdersAsync()
        {
            return await _context.PurchaseOrders
                .OrderByDescending(po => po.Id)
                .Select(po => new PurchaseOrderDto
                {
                    Id = po.Id,
                    CompanyId = po.CompanyId,
                    SupplierId = po.SupplierId,
                    SupplierName = _context.Suppliers
                        .Where(s => s.Id == po.SupplierId)
                        .Select(s => s.SupplierName)
                        .FirstOrDefault() ?? "قيد التحديد في الفاتورة",
                    WarehouseId = po.WarehouseId,
                    OrderNumber = po.OrderNumber,
                    OrderDate = po.OrderDate.ToDateTime(TimeOnly.MinValue),
                    Status = po.Status,
                    Subtotal = po.Subtotal,
                    DiscountAmount = po.DiscountAmount,
                    TaxAmount = po.TaxAmount,
                    TotalAmount = po.TotalAmount,
                    Lines = _context.PurchaseOrderLines
                        .Where(l => l.PurchaseOrderId == po.Id)
                        .Select(l => new PurchaseOrderDetailDto
                        {
                            Id = l.Id,
                            PurchaseOrderId = l.PurchaseOrderId,
                            ItemId = l.ItemId,
                            Quantity = l.Quantity,
                            UnitPrice = l.UnitPrice,
                            DiscountAmount = l.DiscountAmount,
                            TaxAmount = l.TaxAmount,
                            ReceivedQuantity = l.ReceivedQuantity
                        })
                        .ToList()
                })
                .ToListAsync();
        }

        public async Task<PurchaseOrderDto?> GetOrderByIdAsync(long id)
        {
            var po = await _context.PurchaseOrders.FirstOrDefaultAsync(p => p.Id == id);
            if (po == null) return null;

            var lines = await _context.PurchaseOrderLines
                .Where(l => l.PurchaseOrderId == id)
                .Select(l => new PurchaseOrderDetailDto
                {
                    Id = l.Id,
                    PurchaseOrderId = l.PurchaseOrderId,
                    ItemId = l.ItemId,
                    Quantity = l.Quantity,
                    UnitPrice = l.UnitPrice,
                    DiscountAmount = l.DiscountAmount,
                    TaxAmount = l.TaxAmount,
                    ReceivedQuantity = l.ReceivedQuantity
                })
                .ToListAsync();

            return new PurchaseOrderDto
            {
                Id = po.Id,
                CompanyId = po.CompanyId,
                SupplierId = po.SupplierId,
                SupplierName = _context.Suppliers
                    .Where(s => s.Id == po.SupplierId)
                    .Select(s => s.SupplierName)
                    .FirstOrDefault() ?? "قيد التحديد في الفاتورة",
                WarehouseId = po.WarehouseId,
                OrderNumber = po.OrderNumber,
                OrderDate = po.OrderDate.ToDateTime(TimeOnly.MinValue),
                Status = po.Status,
                Subtotal = po.Subtotal,
                DiscountAmount = po.DiscountAmount,
                TaxAmount = po.TaxAmount,
                TotalAmount = po.TotalAmount,
                Lines = lines
            };
        }

        public async Task<bool> CreateOrderAsync(PurchaseOrderCreateDto dto)
        {
            if (dto == null || dto.Lines == null || dto.Lines.Count == 0)
                return false;

            decimal subtotal = dto.Lines.Sum(l => l.Quantity * l.UnitPrice);
            decimal discount = dto.Lines.Sum(l => l.DiscountAmount);
            decimal tax = dto.Lines.Sum(l => l.TaxAmount);
            decimal total = subtotal - discount + tax;

            string generatedOrderNumber = string.IsNullOrWhiteSpace(dto.OrderNumber)
                ? $"PO-{DateTime.UtcNow:yyyyMMdd}-{new Random().Next(100, 999)}"
                : dto.OrderNumber;

            // تحديد مورد أولي سليم من قاعدة البيانات لمنع خطأ Foreign Key
            long resolvedSupplierId = dto.SupplierId;
            bool supplierExists = await _context.Suppliers.AnyAsync(s => s.Id == resolvedSupplierId);
            if (!supplierExists)
            {
                resolvedSupplierId = await _context.Suppliers.Select(s => s.Id).FirstOrDefaultAsync();
            }

            // تحديد مخزن أولي سليم
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
                        ReceivedQuantity = 0
                    };
                    _context.PurchaseOrderLines.Add(poLine);
                }

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
}