using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AbuAdelERP.Api.DTOs;
using AbuAdelERP.Api.Models;
using AbuAdelERP.Api.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AbuAdelERP.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PurchaseOrdersController : ControllerBase
    {
        private readonly IPurchaseOrderService _orderService;
        private readonly AppDbContext _context;

        public PurchaseOrdersController(IPurchaseOrderService orderService, AppDbContext context)
        {
            _orderService = orderService;
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<List<PurchaseOrderDto>>> GetOrders()
        {
            var orders = await _orderService.GetAllOrdersAsync();
            return Ok(orders);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<PurchaseOrderDto>> GetOrder(long id)
        {
            var order = await _orderService.GetOrderByIdAsync(id);
            if (order == null) return NotFound("أمر الشراء غير موجود.");
            return Ok(order);
        }

        [HttpPost]
        public async Task<ActionResult<bool>> CreateOrder([FromBody] PurchaseOrderCreateDto dto)
        {
            var result = await _orderService.CreateOrderAsync(dto);
            if (!result) return BadRequest("فشل إنشاء أمر الشراء. تحقق من صحة البنود.");
            return Ok(true);
        }

        [HttpGet("recommended-pricing/{itemId}")]
        public async Task<IActionResult> GetRecommendedPricing(long itemId)
        {
            var pricing = await _context.SupplierItemPrices
                .Include(p => p.Supplier)
                .Where(p => p.ItemId == itemId && p.Supplier != null && p.Supplier.Active)
                .OrderByDescending(p => p.IsPreferred)
                .ThenBy(p => p.Price)
                .Select(p => new
                {
                    SupplierId = p.SupplierId,
                    SupplierName = p.Supplier!.SupplierName,
                    IsApproved = p.Supplier.IsApproved,
                    Price = p.Price,
                    Currency = p.Currency,
                    LeadTimeDays = p.LeadTimeDays,
                    IsPreferred = p.IsPreferred
                })
                .FirstOrDefaultAsync();

            if (pricing == null)
            {
                return Ok(new { HasPrice = false, Price = 0m, SupplierId = 0L, SupplierName = "" });
            }

            return Ok(new
            {
                HasPrice = true,
                SupplierId = pricing.SupplierId,
                SupplierName = pricing.SupplierName,
                IsApproved = pricing.IsApproved,
                Price = pricing.Price,
                Currency = pricing.Currency,
                LeadTimeDays = pricing.LeadTimeDays,
                IsPreferred = pricing.IsPreferred
            });
        }

        [HttpPatch("{id}/status")]
        public async Task<IActionResult> UpdateOrderStatus(long id, [FromQuery] string newStatus)
        {
            var order = await _context.PurchaseOrders.FindAsync(id);
            if (order == null) return NotFound("أمر الشراء غير موجود.");

            order.Status = newStatus;
            order.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return Ok(new { Message = "تم تحديث حالة أمر الشراء بنجاح." });
        }

        // تقسيم واعتماد الفواتير آلياً بحسب الموردين
        [HttpPost("approve-invoice")]
        public async Task<IActionResult> ApproveInvoice([FromBody] ApproveInvoiceRequestDto dto)
        {
            if (dto == null || dto.OrderId <= 0 || dto.Lines == null || dto.Lines.Count == 0)
                return BadRequest("بيانات الاعتماد غير صالحة أو لا تحتوي على أصناف.");

            var originalOrder = await _context.PurchaseOrders.FirstOrDefaultAsync(o => o.Id == dto.OrderId);
            if (originalOrder == null)
                return NotFound("أمر الشراء المطلوب اعتماده غير موجود.");

            // تجميع البنود حسب المورد المختار لكل صنف
            var supplierGroups = dto.Lines.GroupBy(l => l.SupplierId).ToList();

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                int groupIndex = 1;

                foreach (var group in supplierGroups)
                {
                    long supplierId = group.Key;
                    var groupLines = group.ToList();

                    decimal subtotal = groupLines.Sum(l => l.Quantity * l.UnitPrice);
                    decimal discount = 0;
                    decimal tax = 0;
                    decimal total = subtotal - discount + tax;

                    if (groupIndex == 1)
                    {
                        // استخدام السجل الأصلي لأول مورد لتفادي تكرار السجلات دون داعٍ
                        originalOrder.SupplierId = supplierId;
                        originalOrder.WarehouseId = dto.WarehouseId > 0 ? dto.WarehouseId : originalOrder.WarehouseId;
                        originalOrder.Subtotal = subtotal;
                        originalOrder.DiscountAmount = discount;
                        originalOrder.TaxAmount = tax;
                        originalOrder.TotalAmount = total;
                        originalOrder.Status = "Approved";
                        originalOrder.UpdatedAt = DateTime.UtcNow;

                        // حذف السطور القديمة لأمر الشراء وتثبيت السطور الخاصة بهذا المورد فقط
                        var oldLines = await _context.PurchaseOrderLines
                            .Where(l => l.PurchaseOrderId == originalOrder.Id)
                            .ToListAsync();
                        _context.PurchaseOrderLines.RemoveRange(oldLines);

                        foreach (var gLine in groupLines)
                        {
                            _context.PurchaseOrderLines.Add(new PurchaseOrderLine
                            {
                                PurchaseOrderId = originalOrder.Id,
                                ItemId = gLine.ItemId,
                                Quantity = gLine.Quantity,
                                UnitPrice = gLine.UnitPrice,
                                DiscountAmount = 0,
                                TaxAmount = 0,
                                ReceivedQuantity = 0
                            });
                        }
                    }
                    else
                    {
                        // إنشاء فاتورة جديدة للموردين الآخرين
                        string newOrderNumber = $"{originalOrder.OrderNumber}-S{groupIndex}";
                        var newOrder = new PurchaseOrder
                        {
                            CompanyId = originalOrder.CompanyId,
                            SupplierId = supplierId,
                            WarehouseId = dto.WarehouseId > 0 ? dto.WarehouseId : originalOrder.WarehouseId,
                            OrderNumber = newOrderNumber,
                            OrderDate = originalOrder.OrderDate,
                            Status = "Approved",
                            Subtotal = subtotal,
                            DiscountAmount = discount,
                            TaxAmount = tax,
                            TotalAmount = total,
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow
                        };

                        _context.PurchaseOrders.Add(newOrder);
                        await _context.SaveChangesAsync();

                        foreach (var gLine in groupLines)
                        {
                            _context.PurchaseOrderLines.Add(new PurchaseOrderLine
                            {
                                PurchaseOrderId = newOrder.Id,
                                ItemId = gLine.ItemId,
                                Quantity = gLine.Quantity,
                                UnitPrice = gLine.UnitPrice,
                                DiscountAmount = 0,
                                TaxAmount = 0,
                                ReceivedQuantity = 0
                            });
                        }
                    }

                    groupIndex++;
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Ok(new { Message = $"تم تقسيم الفاتورة واعتمادها بنجاح لعدد {supplierGroups.Count} موردين!" });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, $"خطأ أثناء التقسيم والاعتماد: {ex.Message}");
            }
        }
    }

    public class ApproveInvoiceRequestDto
    {
        public long OrderId { get; set; }
        public long WarehouseId { get; set; }
        public string Status { get; set; } = "Approved";
        public List<ApproveInvoiceLineDto> Lines { get; set; } = new();
    }

    public class ApproveInvoiceLineDto
    {
        public long LineId { get; set; }
        public long ItemId { get; set; }
        public long SupplierId { get; set; }
        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }
    }
}