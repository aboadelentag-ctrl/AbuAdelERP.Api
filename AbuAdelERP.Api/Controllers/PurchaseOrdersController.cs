using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AbuAdelERP.Api.Models;
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
        private readonly AppDbContext _context;

        public PurchaseOrdersController(AppDbContext context)
        {
            _context = context;
        }

        // ==============================================================
        // 1. جلب كافة أوامر الشراء وطلبات الاحتياج مع الملاحظات
        // ==============================================================
        [HttpGet]
        public async Task<IActionResult> GetOrders()
        {
            try
            {
                var orders = await _context.PurchaseOrders
                    .Include(p => p.Supplier)
                    .Include(p => p.PurchaseOrderLines)
                        .ThenInclude(l => l.Item)
                    .AsNoTracking()
                    .OrderByDescending(p => p.Id)
                    .Select(p => new
                    {
                        p.Id,
                        p.CompanyId,
                        p.SupplierId,
                        SupplierName = p.Supplier != null ? p.Supplier.SupplierName : "غير محدد",
                        p.WarehouseId,
                        p.OrderNumber,
                        OrderDate = p.OrderDate.ToString("yyyy-MM-dd"),
                        p.Status,
                        p.TotalAmount,
                        Lines = p.PurchaseOrderLines.Select(l => new
                        {
                            l.Id,
                            l.ItemId,
                            ItemCode = l.Item != null ? l.Item.ItemCode : "",
                            ItemName = l.Item != null ? l.Item.ItemName : "",
                            Unit = l.Item != null ? l.Item.Unit : "",
                            l.Quantity,
                            l.UnitPrice,
                            l.ReceivedQuantity,
                            l.LineTotal,
                            l.Notes
                        }).ToList()
                    })
                    .ToListAsync();

                return Ok(orders);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"خطأ في جلب الأوامر: {ex.GetBaseException().Message}");
            }
        }

        // ==============================================================
        // 2. جلب أمر شراء محدد
        // ==============================================================
        [HttpGet("{id}")]
        public async Task<IActionResult> GetOrder(long id)
        {
            try
            {
                var order = await _context.PurchaseOrders
                    .Include(p => p.Supplier)
                    .Include(p => p.PurchaseOrderLines)
                        .ThenInclude(l => l.Item)
                    .FirstOrDefaultAsync(p => p.Id == id);

                if (order == null) return NotFound("أمر الشراء غير موجود.");

                return Ok(new
                {
                    order.Id,
                    order.CompanyId,
                    order.SupplierId,
                    SupplierName = order.Supplier != null ? order.Supplier.SupplierName : "غير محدد",
                    order.WarehouseId,
                    order.OrderNumber,
                    OrderDate = order.OrderDate.ToString("yyyy-MM-dd"),
                    order.Status,
                    order.TotalAmount,
                    Lines = order.PurchaseOrderLines.Select(l => new
                    {
                        l.Id,
                        l.ItemId,
                        ItemCode = l.Item != null ? l.Item.ItemCode : "",
                        ItemName = l.Item != null ? l.Item.ItemName : "",
                        Unit = l.Item != null ? l.Item.Unit : "",
                        l.Quantity,
                        l.UnitPrice,
                        l.ReceivedQuantity,
                        l.LineTotal,
                        l.Notes
                    }).ToList()
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"خطأ: {ex.GetBaseException().Message}");
            }
        }

        // ==============================================================
        // 3. إصدار طلب احتياج جديد (معالجة السطر 139 لتحويل DateOnly بأمان)
        // ==============================================================
        [HttpPost]
        public async Task<IActionResult> CreateOrder([FromBody] PoCreateRequestDto dto)
        {
            if (dto == null || dto.Lines == null || !dto.Lines.Any())
                return BadRequest("يجب إضافة بنود للطلب أولاً.");

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var companyId = dto.CompanyId > 0 ? dto.CompanyId : 1;
                var warehouseId = dto.WarehouseId > 0 ? dto.WarehouseId : 1;
                var orderNum = !string.IsNullOrWhiteSpace(dto.OrderNumber)
                    ? dto.OrderNumber.Trim()
                    : $"PO-{DateTime.Now:yyyyMMdd}-{new Random().Next(100, 999)}";

                var po = new PurchaseOrder
                {
                    CompanyId = companyId,
                    SupplierId = dto.SupplierId,
                    WarehouseId = warehouseId,
                    OrderNumber = orderNum,
                    OrderDate = DateOnly.FromDateTime(dto.OrderDate), // معالجة السطر 139 بنجاح
                    Status = string.IsNullOrWhiteSpace(dto.Status) ? "Pending_Procurement" : dto.Status,
                    TotalAmount = dto.Lines.Sum(l => l.Quantity * l.UnitPrice),
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
                        ReceivedQuantity = 0,
                        DiscountAmount = 0,
                        TaxAmount = 0,
                        LineTotal = line.Quantity * line.UnitPrice,
                        Notes = line.Notes?.Trim()
                    };
                    _context.PurchaseOrderLines.Add(poLine);
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Ok(new { success = true, id = po.Id, orderNumber = po.OrderNumber });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, $"خطأ أثناء حفظ أمر الشراء: {ex.GetBaseException().Message}");
            }
        }

        // ==============================================================
        // 4. مسار اعتماد الفاتورة وتخصيص الموردين والأسعار
        // ==============================================================
        [HttpPost("approve-invoice")]
        public async Task<IActionResult> ApproveInvoice([FromBody] PoApproveDto dto)
        {
            if (dto == null || dto.OrderId <= 0 || dto.Lines == null || !dto.Lines.Any())
                return BadRequest("بيانات غير صالحة للاعتماد.");

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var order = await _context.PurchaseOrders
                    .Include(p => p.PurchaseOrderLines)
                    .FirstOrDefaultAsync(p => p.Id == dto.OrderId);

                if (order == null) return NotFound("أمر الشراء غير موجود.");

                order.Status = "Approved";
                order.UpdatedAt = DateTime.UtcNow;

                decimal total = 0;
                foreach (var editLine in dto.Lines)
                {
                    var existingLine = order.PurchaseOrderLines.FirstOrDefault(l => l.Id == editLine.LineId || l.ItemId == editLine.ItemId);
                    if (existingLine != null)
                    {
                        existingLine.UnitPrice = editLine.UnitPrice;
                        existingLine.Quantity = editLine.Quantity;
                        existingLine.LineTotal = editLine.Quantity * editLine.UnitPrice;
                        if (!string.IsNullOrWhiteSpace(editLine.Notes))
                            existingLine.Notes = editLine.Notes;

                        total += existingLine.LineTotal;
                    }
                }

                order.TotalAmount = total;
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Ok(new { success = true, message = "تم اعتماد أمر الشراء والفاتورة بنجاح!" });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, $"خطأ أثناء اعتماد الفاتورة: {ex.GetBaseException().Message}");
            }
        }

        // ==============================================================
        // DTOs
        // ==============================================================
        public class PoCreateRequestDto
        {
            public long CompanyId { get; set; } = 1;
            public long SupplierId { get; set; } = 0;
            public long WarehouseId { get; set; } = 1;
            public string OrderNumber { get; set; } = string.Empty;
            public DateTime OrderDate { get; set; } = DateTime.Today;
            public string Status { get; set; } = "Pending_Procurement";
            public List<PoLineInputDto> Lines { get; set; } = new();
        }

        public class PoLineInputDto
        {
            public long ItemId { get; set; }
            public decimal Quantity { get; set; }
            public decimal UnitPrice { get; set; } = 0;
            public string? Notes { get; set; }
        }

        public class PoApproveDto
        {
            public long OrderId { get; set; }
            public string Status { get; set; } = "Approved";
            public List<PoApproveLineDto> Lines { get; set; } = new();
        }

        public class PoApproveLineDto
        {
            public long LineId { get; set; }
            public long ItemId { get; set; }
            public long SupplierId { get; set; }
            public decimal Quantity { get; set; }
            public decimal UnitPrice { get; set; }
            public string? Notes { get; set; }
        }
    }
}