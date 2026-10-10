using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AbuAdelERP.Api.Models;

namespace AbuAdelERP.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class StockCardController : ControllerBase
    {
        private readonly AppDbContext _context;

        public StockCardController(AppDbContext context)
        {
            _context = context;
        }

        // ==============================================================
        // 1. القوائم المرجعية لكارت الصنف (الأصناف والمستودعات بالفروع)
        // ==============================================================
        [HttpGet("lookups")]
        public async Task<IActionResult> GetLookups()
        {
            try
            {
                var items = await _context.Items
                    .AsNoTracking()
                    .Where(i => i.Active)
                    .OrderBy(i => i.ItemName)
                    .Select(i => new
                    {
                        i.Id,
                        i.ItemCode,
                        i.ItemName,
                        i.Unit,
                        i.ItemType,
                        CategoryName = i.ItemCategory != null ? i.ItemCategory.CategoryName : "عام"
                    })
                    .ToListAsync();

                var warehouses = await _context.Warehouses
                    .Include(w => w.Branch)
                    .AsNoTracking()
                    .Where(w => w.Active)
                    .OrderBy(w => w.WarehouseName)
                    .Select(w => new
                    {
                        w.Id,
                        w.WarehouseCode,
                        WarehouseName = w.Branch != null
                            ? $"{w.WarehouseName} - [{w.Branch.BranchName}]"
                            : w.WarehouseName
                    })
                    .ToListAsync();

                return Ok(new { items, warehouses });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"خطأ في جلب البيانات المرجعية: {ex.GetBaseException().Message}");
            }
        }

        // ==============================================================
        // 2. كشف حركة كارت الصنف واحتساب الرصيد التراكمي اللحظي
        // ==============================================================
        [HttpGet("ledger")]
        public async Task<IActionResult> GetStockLedger(
            [FromQuery] long itemId,
            [FromQuery] long? warehouseId = null,
            [FromQuery] DateTime? fromDate = null,
            [FromQuery] DateTime? toDate = null)
        {
            if (itemId <= 0)
                return BadRequest("يجب تحديد الصنف لعرض كارت الحركة.");

            try
            {
                var item = await _context.Items
                    .Include(i => i.ItemCategory)
                    .AsNoTracking()
                    .FirstOrDefaultAsync(i => i.Id == itemId);

                if (item == null)
                    return NotFound("الصنف غير موجود بالنظام.");

                var baseMovementsQuery = _context.StockMovements
                    .Include(m => m.Warehouse)
                        .ThenInclude(w => w.Branch)
                    .AsNoTracking()
                    .Where(m => m.ItemId == itemId);

                if (warehouseId.HasValue && warehouseId.Value > 0)
                {
                    baseMovementsQuery = baseMovementsQuery.Where(m => m.WarehouseId == warehouseId.Value);
                }

                // 1. احتساب رصيد أول المدة بدقة متناهية قبل تاريخ البداية
                decimal openingBalance = 0;
                var periodQuery = baseMovementsQuery;

                if (fromDate.HasValue)
                {
                    DateTime startUtc = DateTime.SpecifyKind(fromDate.Value.Date, DateTimeKind.Utc);

                    var priorMovements = await baseMovementsQuery
                        .Where(m => m.MovementAt < startUtc)
                        .Select(m => new { m.MovementType, m.Quantity })
                        .ToListAsync();

                    foreach (var m in priorMovements)
                    {
                        if (string.Equals(m.MovementType, "QC_REJECTED", StringComparison.OrdinalIgnoreCase))
                            continue; // المرفوضات لا تؤثر على الرصيد

                        decimal qty = Math.Abs(m.Quantity);
                        openingBalance += IsInbound(m.MovementType) ? qty : -qty;
                    }

                    periodQuery = periodQuery.Where(m => m.MovementAt >= startUtc);
                }

                if (toDate.HasValue)
                {
                    DateTime endUtc = DateTime.SpecifyKind(toDate.Value.Date.AddDays(1).AddTicks(-1), DateTimeKind.Utc);
                    periodQuery = periodQuery.Where(m => m.MovementAt <= endUtc);
                }

                // 2. جلب الحركات داخل الفترة المحددة
                var periodMovements = await periodQuery
                    .OrderBy(m => m.MovementAt)
                    .ThenBy(m => m.Id)
                    .ToListAsync();

                // 3. بناء سطور الكشف وتوليد الرصيد التراكمي المتسلسل (Running Balance)
                decimal runningBalance = openingBalance;
                var ledgerLines = new List<StockLedgerLineDto>();

                foreach (var m in periodMovements)
                {
                    // استخراج رقم اللوط وتاريخ الصلاحية
                    string batchNumber = ExtractValue(m.Notes, "تشغيلة:", "|");
                    if (string.IsNullOrEmpty(batchNumber)) batchNumber = ExtractValue(m.Notes, "لوط:", "|");
                    if (string.IsNullOrEmpty(batchNumber)) batchNumber = ExtractValue(m.Notes, "LOT:", "|");
                    if (string.IsNullOrEmpty(batchNumber)) batchNumber = ExtractValue(m.Notes, "تشغيلة/تتبع:", "|");

                    string expiryDate = ExtractValue(m.Notes, "صلاحية:", "|");

                    // معالجة المرفوضات الفنية: تظهر للمعاينة والرقابة لكنها لا تؤثر على الرصيد المخزني
                    if (string.Equals(m.MovementType, "QC_REJECTED", StringComparison.OrdinalIgnoreCase))
                    {
                        ledgerLines.Add(new StockLedgerLineDto
                        {
                            MovementId = m.Id,
                            MovementDate = m.MovementAt,
                            MovementType = m.MovementType,
                            MovementTypeLabel = "فحص جودة - مرفوض (خارج المخزون)",
                            WarehouseName = m.Warehouse != null ? m.Warehouse.WarehouseName : "مستودع غير محدد",
                            BranchName = m.Warehouse?.Branch != null ? m.Warehouse.Branch.BranchName : "الفرع الرئيسي",
                            InQuantity = 0,
                            OutQuantity = 0,
                            BalanceAfter = runningBalance,
                            UnitCost = m.UnitCost,
                            TotalCost = 0,
                            BatchNumber = !string.IsNullOrEmpty(batchNumber) ? batchNumber : "---",
                            ExpiryDate = !string.IsNullOrEmpty(expiryDate) ? expiryDate : "---",
                            ReferenceType = m.ReferenceType ?? "---",
                            ReferenceId = m.ReferenceId,
                            Notes = m.Notes
                        });
                        continue;
                    }

                    bool inbound = IsInbound(m.MovementType);
                    decimal qty = Math.Abs(m.Quantity);
                    decimal inQty = inbound ? qty : 0;
                    decimal outQty = !inbound ? qty : 0;

                    runningBalance += (inQty - outQty);

                    ledgerLines.Add(new StockLedgerLineDto
                    {
                        MovementId = m.Id,
                        MovementDate = m.MovementAt,
                        MovementType = m.MovementType,
                        MovementTypeLabel = GetMovementTypeLabel(m.MovementType),
                        WarehouseName = m.Warehouse != null ? m.Warehouse.WarehouseName : "مستودع غير محدد",
                        BranchName = m.Warehouse?.Branch != null ? m.Warehouse.Branch.BranchName : "الفرع الرئيسي",
                        InQuantity = inQty,
                        OutQuantity = outQty,
                        BalanceAfter = runningBalance,
                        UnitCost = m.UnitCost,
                        TotalCost = qty * m.UnitCost,
                        BatchNumber = !string.IsNullOrEmpty(batchNumber) ? batchNumber : "---",
                        ExpiryDate = !string.IsNullOrEmpty(expiryDate) ? expiryDate : "---",
                        ReferenceType = m.ReferenceType ?? "---",
                        ReferenceId = m.ReferenceId,
                        Notes = m.Notes
                    });
                }

                var result = new StockCardReportDto
                {
                    ItemId = item.Id,
                    ItemCode = item.ItemCode,
                    ItemName = item.ItemName,
                    Unit = item.Unit ?? "قطعة",
                    CategoryName = item.ItemCategory != null ? item.ItemCategory.CategoryName : "عام",
                    OpeningBalance = openingBalance,
                    TotalInbound = ledgerLines.Sum(l => l.InQuantity),
                    TotalOutbound = ledgerLines.Sum(l => l.OutQuantity),
                    ClosingBalance = runningBalance,
                    Lines = ledgerLines
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"خطأ في توليد كارت الصنف: {ex.GetBaseException().Message}");
            }
        }

        // ==============================================================
        // دوال مساعدة في التصنيف والتحليل
        // ==============================================================
        private static bool IsInbound(string movementType) => movementType switch
        {
            "PURCHASE_RECEIPT" => true,
            "TRANSFER_IN" => true,
            "FG_TRANSFER_IN" => true,
            "PRODUCTION_IN" => true,
            "PRODUCTION_RECEIPT" => true,
            "INITIAL_STOCK" => true,
            "STOCK_IN" => true,
            "ADJUSTMENT_IN" => true,
            _ => false
        };

        private static string GetMovementTypeLabel(string movementType) => movementType switch
        {
            "PURCHASE_RECEIPT" => "استلام شراء (توريد خامات)",
            "SALES_ISSUE" => "صرف فاتورة بيع",
            "TRANSFER_OUT" => "تحويل مخزني (صادر)",
            "TRANSFER_IN" => "تحويل مخزني (وارد)",
            "TRANSFER_LOSS" => "هالك / عجز تحويل",
            "PRODUCTION_ISSUE" => "صرف خامات تشغيل للوردية",
            "PRODUCTION_IN" => "استلام إنتاج تام",
            "PRODUCTION_RECEIPT" => "استلام تشغيل صالة الإنتاج",
            "FG_TRANSFER_OUT" => "خروج تحويل منتج تام من الصالة",
            "FG_TRANSFER_IN" => "استلام منتج تام بمخزن الكنترول",
            "QC_SAMPLE" => "سحب عينات فحص الجودة المعملي",
            "QC_SCRAP" => "إهلاك وتالف فني غير مطابق للجودة",
            "QC_REJECTED" => "فحص جودة - مرفوض (خارج المخزون)",
            "FG_TRANSIT_DEFICIT" => "تسوية عجز استلام كنترول",
            "ADJUSTMENT_IN" => "تسوية جردية (إضافة)",
            "ADJUSTMENT_OUT" => "تسوية جردية (خصم)",
            _ => movementType
        };

        private static string ExtractValue(string? text, string key, string separator)
        {
            if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(key)) return string.Empty;
            int startIdx = text.IndexOf(key, StringComparison.OrdinalIgnoreCase);
            if (startIdx == -1) return string.Empty;

            startIdx += key.Length;
            if (startIdx >= text.Length) return string.Empty;

            int endIdx = text.IndexOf(separator, startIdx, StringComparison.OrdinalIgnoreCase);
            if (endIdx == -1) endIdx = text.Length;

            return text.Substring(startIdx, endIdx - startIdx).Trim();
        }
    }

    // ==============================================================
    // نماذج البيانات لكارت الصنف (DTOs)
    // ==============================================================
    public class StockCardReportDto
    {
        public long ItemId { get; set; }
        public string ItemCode { get; set; } = string.Empty;
        public string ItemName { get; set; } = string.Empty;
        public string Unit { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public decimal OpeningBalance { get; set; }
        public decimal TotalInbound { get; set; }
        public decimal TotalOutbound { get; set; }
        public decimal ClosingBalance { get; set; }
        public List<StockLedgerLineDto> Lines { get; set; } = new();
    }

    public class StockLedgerLineDto
    {
        public long MovementId { get; set; }
        public DateTime MovementDate { get; set; }
        public string MovementType { get; set; } = string.Empty;
        public string MovementTypeLabel { get; set; } = string.Empty;
        public string WarehouseName { get; set; } = string.Empty;
        public string BranchName { get; set; } = string.Empty;
        public decimal InQuantity { get; set; }
        public decimal OutQuantity { get; set; }
        public decimal BalanceAfter { get; set; }
        public decimal UnitCost { get; set; }
        public decimal TotalCost { get; set; }
        public string BatchNumber { get; set; } = string.Empty;
        public string ExpiryDate { get; set; } = string.Empty;
        public string ReferenceType { get; set; } = string.Empty;
        public long? ReferenceId { get; set; }
        public string? Notes { get; set; }
    }
}