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
    public class MaterialReceiptController : ControllerBase
    {
        private readonly AppDbContext _context;

        public MaterialReceiptController(AppDbContext context)
        {
            _context = context;
        }

        // ==============================================================
        // 1. جلب الفواتير المعتمدة المفتوحة فقط (استبعاد المغلقة والمرفوضة والتي رصيدها 0)
        // ==============================================================
        [HttpGet("pending-invoices")]
        public async Task<IActionResult> GetPendingInvoices([FromQuery] long? supplierId = null)
        {
            try
            {
                var query = _context.PurchaseOrders
                    .Include(p => p.Supplier)
                    .Include(p => p.PurchaseOrderLines)
                        .ThenInclude(l => l.Item)
                    .AsNoTracking()
                    .Where(p => p.Status == "Approved" || p.Status == "Partially_Received");

                if (supplierId.HasValue && supplierId.Value > 0)
                    query = query.Where(p => p.SupplierId == supplierId.Value);

                // استبعاد أي أمر شراء ليس به أي رصيد متبقي للاستلام
                query = query.Where(p => p.PurchaseOrderLines.Any(l => (l.Quantity - l.ReceivedQuantity) > 0));

                var list = await query
                    .OrderByDescending(p => p.Id)
                    .Select(p => new
                    {
                        p.Id,
                        p.OrderNumber,
                        OrderDate = p.OrderDate.ToString("yyyy-MM-dd"),
                        p.SupplierId,
                        SupplierName = p.Supplier != null ? p.Supplier.SupplierName : "غير محدد",
                        p.WarehouseId,
                        p.TotalAmount,
                        p.Status,
                        Lines = p.PurchaseOrderLines
                            .Where(l => (l.Quantity - l.ReceivedQuantity) > 0) // جلب البنود المتبقية فقط
                            .Select(l => new
                            {
                                l.Id,
                                l.ItemId,
                                ItemCode = l.Item != null ? l.Item.ItemCode : "",
                                ItemName = l.Item != null ? l.Item.ItemName : "",
                                Unit = l.Item != null ? l.Item.Unit : "",
                                l.Quantity,
                                l.ReceivedQuantity,
                                RemainingQuantity = (l.Quantity - l.ReceivedQuantity),
                                l.UnitPrice,
                                l.Notes
                            }).ToList()
                    })
                    .ToListAsync();

                return Ok(list);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"خطأ في جلب الفواتير المعتمدة: {ex.GetBaseException().Message}");
            }
        }

        // ==============================================================
        // 2. تأكيد الاستلام أو الرفض وإغلاق الفاتورة فورياً وحجبها
        // ==============================================================
        [HttpPost]
        public async Task<IActionResult> PostMaterialReceipt([FromBody] MaterialReceiptPostDto dto)
        {
            if (dto == null || dto.Items == null || !dto.Items.Any())
                return BadRequest("لا توجد بنود للاستلام أو الفحص.");

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var order = await _context.PurchaseOrders
                    .Include(p => p.PurchaseOrderLines)
                    .FirstOrDefaultAsync(p => p.Id == dto.InvoiceId);

                if (order == null)
                    return NotFound("فاتورة / أمر الشراء غير موجود.");

                if (order.Status == "Closed" || order.Status == "Received" || order.Status == "Rejected")
                    return BadRequest("هذه الفاتورة مغلقة أو مرفوضة بالكامل مسبقاً، ولا يمكن إعادة فحصها.");

                var targetWarehouseId = dto.WarehouseId > 0 ? dto.WarehouseId : order.WarehouseId;
                var now = DateTime.UtcNow;

                decimal totalAcceptedInReceipt = dto.Items.Sum(x => x.AcceptedQuantity);
                decimal totalRejectedInReceipt = dto.Items.Sum(x => x.RejectedQuantity);

                foreach (var item in dto.Items)
                {
                    var poLine = order.PurchaseOrderLines.FirstOrDefault(l => l.ItemId == item.ItemId);
                    if (poLine != null)
                    {
                        // زيادة الكمية المقبولة المستلمة
                        poLine.ReceivedQuantity += item.AcceptedQuantity;
                    }

                    // 1. ترحيل الرصيد المقبول إلى جدول المخازن (StockBalances)
                    if (item.AcceptedQuantity > 0)
                    {
                        var balance = await _context.StockBalances
                            .FirstOrDefaultAsync(b => b.WarehouseId == targetWarehouseId && b.ItemId == item.ItemId);

                        if (balance != null)
                        {
                            balance.Quantity += item.AcceptedQuantity;
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
                                ReservedQuantity = 0,
                                LastUpdatedAt = now
                            });
                        }

                        // تسجيل حركة الوارد في كارت الصنف
                        var qcNote = $"LOT: {item.BatchNumber} | صلاحية: {item.ExpiryDate:yyyy-MM-dd} | قرار: مقبول | مفتش: {dto.InspectorName} | COA: {dto.CoaNumber}";
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

                    // 2. توثيق الكميات المرفوضة في سجل الجودة الرقابي (دون التأثير على رصيد المخزن)
                    if (item.RejectedQuantity > 0)
                    {
                        var rejectNote = $"LOT: {item.BatchNumber} | مرفوض فني | السبب: {item.RejectionReason} | المفتش: {dto.InspectorName}";
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

                // ==============================================================
                // 3. المحرك الحسابي لإغلاق الفاتورة (Closing Engine)
                // ==============================================================
                string finalMessage = "";

                // الحالة أ: الفاتورة رُفضت بالكامل (لم يُقبل أي صنف منها)
                if (totalAcceptedInReceipt == 0 && totalRejectedInReceipt > 0 && order.PurchaseOrderLines.All(l => l.ReceivedQuantity == 0))
                {
                    order.Status = "Rejected"; // مرفوضة ومغلقة نهائياً
                    finalMessage = $"تم تسجيل رفض الشحنة بالكامل وإغلاق الفاتورة رقم [{order.OrderNumber}] وإخراجها من قائمة الاستلام.";
                }
                // الحالة ب: استلام كامل أو تم فحص وتصفية كامل الكميات المتبقية (مقبول + مرفوض)
                else if (order.PurchaseOrderLines.All(l => l.ReceivedQuantity >= l.Quantity) ||
                         dto.Items.All(i => i.QcDecision == "Rejected" || i.AcceptedQuantity > 0))
                {
                    order.Status = "Closed"; // مغلقة ومستلمة بالكامل
                    finalMessage = $"تم اعتماد إذن الفحص وترحيل الرصيد وإغلاق الفاتورة رقم [{order.OrderNumber}] نهائياً بنجاح!";
                }
                // الحالة ج: استلام جزئي وما زال هناك خامات متبقية للتوريد
                else
                {
                    order.Status = "Partially_Received";
                    finalMessage = $"تم إثبات الاستلام الجزئي للفاتورة رقم [{order.OrderNumber}]، وتحديث الكميات المتبقية للتوريدات القادمة.";
                }

                order.UpdatedAt = now;

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Ok(new
                {
                    success = true,
                    newStatus = order.Status,
                    message = finalMessage
                });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, $"خطأ أثناء حفظ الاستلام المخزني: {ex.GetBaseException().Message}");
            }
        }
    }

    public class MaterialReceiptPostDto
    {
        public long SupplierId { get; set; }
        public long InvoiceId { get; set; }
        public long WarehouseId { get; set; }
        public string InspectorName { get; set; } = string.Empty;
        public string CoaNumber { get; set; } = string.Empty;
        public List<ReceiptItemInputDto> Items { get; set; } = new();
    }

    public class ReceiptItemInputDto
    {
        public long ItemId { get; set; }
        public string ItemCode { get; set; } = string.Empty;
        public string ItemName { get; set; } = string.Empty;
        public string BatchNumber { get; set; } = string.Empty;
        public DateTime ExpiryDate { get; set; }
        public decimal ReceivedQuantity { get; set; }
        public decimal AcceptedQuantity { get; set; }
        public decimal RejectedQuantity { get; set; }
        public string QcDecision { get; set; } = "Approved";
        public string RejectionReason { get; set; } = string.Empty;
        public decimal UnitPrice { get; set; }
    }
}