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
    public class InventoryController : ControllerBase
    {
        private readonly AppDbContext _context;

        public InventoryController(AppDbContext context)
        {
            _context = context;
        }

        // ==============================================================
        // 1. ملخص مؤشرات القيادة التنفيذية والتقييم المالي للمخزون
        // ==============================================================
        [HttpGet("dashboard-summary")]
        public async Task<IActionResult> GetDashboardSummary()
        {
            try
            {
                var balances = await _context.StockBalances
                    .Include(b => b.Item)
                    .AsNoTracking()
                    .ToListAsync();

                decimal totalValuation = 0m;
                decimal rawMaterialsValuation = 0m;
                decimal wipValuation = 0m;
                decimal finishedGoodsValuation = 0m;
                int lowStockCount = 0;
                int outOfStockCount = 0;

                foreach (var b in balances)
                {
                    decimal price = b.Item?.BasePrice ?? 0m;
                    decimal qty = b.Quantity > 0 ? b.Quantity : 0m;
                    decimal lineVal = qty * price;
                    totalValuation += lineVal;

                    string itemType = b.Item?.ItemType ?? "خامات";
                    if (itemType.Contains("خام"))
                        rawMaterialsValuation += lineVal;
                    else if (itemType.Contains("نصف") || itemType.Contains("وسيط") || itemType.Contains("عجين"))
                        wipValuation += lineVal;
                    else
                        finishedGoodsValuation += lineVal;

                    decimal available = b.Quantity - b.ReservedQuantity;
                    if (b.Item != null && b.Item.ReorderLevel > 0 && available <= b.Item.ReorderLevel && available > 0)
                        lowStockCount++;

                    if (available <= 0)
                        outOfStockCount++;
                }

                // فحص اللوطات المهددة بانتهاء الصلاحية خلال 45 يوماً
                var today = DateTime.UtcNow.Date;
                var thresholdDate = today.AddDays(45);

                var recentMovements = await _context.StockMovements
                    .AsNoTracking()
                    .Where(m => m.Notes != null && m.Notes.Contains("صلاحية:"))
                    .OrderByDescending(m => m.MovementAt)
                    .Take(500)
                    .Select(m => m.Notes)
                    .ToListAsync();

                int expiringLotsCount = 0;
                var checkedBatches = new HashSet<string>();

                foreach (var note in recentMovements)
                {
                    if (string.IsNullOrEmpty(note)) continue;
                    string expStr = ExtractValue(note, "صلاحية:", "|");
                    string batchNo = ExtractValue(note, "تشغيلة:", "|");
                    if (string.IsNullOrEmpty(batchNo)) batchNo = ExtractValue(note, "لوط:", "|");

                    if (!string.IsNullOrEmpty(expStr) && !string.IsNullOrEmpty(batchNo) && !checkedBatches.Contains(batchNo))
                    {
                        checkedBatches.Add(batchNo);
                        if (DateTime.TryParse(expStr, out DateTime expDate))
                        {
                            if (expDate >= today && expDate <= thresholdDate)
                                expiringLotsCount++;
                        }
                    }
                }

                var summary = new InventoryDashboardSummaryDto
                {
                    TotalInventoryValue = Math.Round(totalValuation, 2),
                    RawMaterialsValue = Math.Round(rawMaterialsValuation, 2),
                    WipValue = Math.Round(wipValuation, 2),
                    FinishedGoodsValue = Math.Round(finishedGoodsValuation, 2),
                    TotalStockRecords = balances.Count,
                    LowStockCount = lowStockCount,
                    OutOfStockCount = outOfStockCount,
                    ExpiringLotsCount = expiringLotsCount
                };

                return Ok(summary);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"خطأ في احتساب ملخص لوحة القيادة: {ex.GetBaseException().Message}");
            }
        }

        // ==============================================================
        // 2. جلب الأرصدة الحالية لجميع الأصناف موزعة على المخازن
        // ==============================================================
        [HttpGet("balances")]
        public async Task<IActionResult> GetBalances([FromQuery] long? warehouseId = null, [FromQuery] string? search = null)
        {
            try
            {
                var query = _context.StockBalances
                    .Include(b => b.Item)
                    .Include(b => b.Warehouse)
                    .AsNoTracking()
                    .AsQueryable();

                if (warehouseId.HasValue && warehouseId.Value > 0)
                    query = query.Where(b => b.WarehouseId == warehouseId.Value);

                if (!string.IsNullOrWhiteSpace(search))
                {
                    var term = search.Trim();
                    query = query.Where(b =>
                        (b.Item != null && (b.Item.ItemName.Contains(term) || b.Item.ItemCode.Contains(term))) ||
                        (b.Warehouse != null && b.Warehouse.WarehouseName.Contains(term)));
                }

                var list = await query
                    .OrderBy(b => b.ItemId)
                    .Select(b => new StockBalanceDetailedDto
                    {
                        Id = b.Id,
                        ItemId = b.ItemId,
                        ItemCode = b.Item != null ? b.Item.ItemCode : "",
                        ItemName = b.Item != null ? b.Item.ItemName : "صنف غير معروف",
                        ItemType = b.Item != null ? b.Item.ItemType : "خامات",
                        Unit = b.Item != null ? b.Item.Unit : "كجم",
                        WarehouseId = b.WarehouseId,
                        WarehouseName = b.Warehouse != null ? b.Warehouse.WarehouseName : "المخزن الرئيسي",
                        Quantity = b.Quantity,
                        ReservedQuantity = b.ReservedQuantity,
                        AvailableQuantity = b.Quantity - b.ReservedQuantity,
                        EstimatedUnitCost = b.Item != null ? b.Item.BasePrice : 0m,
                        TotalValue = (b.Quantity > 0 ? b.Quantity : 0m) * (b.Item != null ? b.Item.BasePrice : 0m),
                        LastUpdatedAt = b.LastUpdatedAt
                    })
                    .ToListAsync();

                return Ok(list);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"خطأ أثناء جلب الأرصدة: {ex.GetBaseException().Message}");
            }
        }

        // ==============================================================
        // 3. تحديث وتعديل أرصدة الأصناف (من شيت الجرد أو الاستيراد)
        // ==============================================================
        [HttpPost("import-balances")]
        public async Task<IActionResult> ImportBalances([FromBody] List<StockBalanceImportDto> items)
        {
            if (items == null || !items.Any())
                return BadRequest("لا توجد بيانات للاستيراد.");

            var company = await _context.Companies.FirstOrDefaultAsync();
            long companyId = company?.Id ?? 1;

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var now = DateTime.UtcNow;
                int updatedCount = 0;
                int createdCount = 0;

                foreach (var dto in items)
                {
                    if (dto.ItemId <= 0 || dto.WarehouseId <= 0) continue;

                    var balance = await _context.StockBalances
                        .FirstOrDefaultAsync(b => b.WarehouseId == dto.WarehouseId && b.ItemId == dto.ItemId);

                    if (balance != null)
                    {
                        balance.Quantity = dto.Quantity;
                        balance.AvailableQuantity = balance.Quantity - balance.ReservedQuantity;
                        balance.LastUpdatedAt = now;
                        updatedCount++;
                    }
                    else
                    {
                        _context.StockBalances.Add(new StockBalance
                        {
                            CompanyId = companyId,
                            WarehouseId = dto.WarehouseId,
                            ItemId = dto.ItemId,
                            Quantity = dto.Quantity,
                            AvailableQuantity = dto.Quantity,
                            ReservedQuantity = 0,
                            LastUpdatedAt = now
                        });
                        createdCount++;
                    }

                    _context.StockMovements.Add(new StockMovement
                    {
                        CompanyId = companyId,
                        WarehouseId = dto.WarehouseId,
                        ItemId = dto.ItemId,
                        MovementType = "STOCK_ADJUSTMENT",
                        Quantity = dto.Quantity,
                        UnitCost = 0,
                        ReferenceType = "EXCEL_IMPORT",
                        Notes = $"تسوية وتحديث رصيد من ملف الإكسيل | {dto.Notes}",
                        MovementAt = now,
                        CreatedAt = now
                    });
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Ok(new { success = true, message = $"اكتمل التحديث: تم تعديل ({updatedCount}) صنف وإضافة ({createdCount}) رصيد مخزني جديد بنجاح." });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, $"فشل الاستيراد: {ex.GetBaseException().Message}");
            }
        }

        // ==============================================================
        // 4. جلب بطاقة حركة الصنف (Item Ledger)
        // ==============================================================
        [HttpGet("ledger/{itemId:long}")]
        public async Task<IActionResult> GetItemLedger(long itemId)
        {
            try
            {
                var movements = await _context.StockMovements
                    .Include(m => m.Warehouse)
                    .Where(m => m.ItemId == itemId)
                    .OrderByDescending(m => m.MovementAt)
                    .Take(250)
                    .Select(m => new StockMovementDto
                    {
                        Id = m.Id,
                        MovementDate = m.MovementAt,
                        MovementType = m.MovementType,
                        WarehouseName = m.Warehouse != null ? m.Warehouse.WarehouseName : "المستودع",
                        Quantity = m.Quantity,
                        UnitCost = m.UnitCost,
                        ReferenceType = m.ReferenceType,
                        ReferenceId = m.ReferenceId,
                        Notes = m.Notes
                    })
                    .ToListAsync();

                return Ok(movements);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"خطأ أثناء جلب حركة الصنف: {ex.GetBaseException().Message}");
            }
        }

        private static string ExtractValue(string text, string key, string separator)
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

    public class InventoryDashboardSummaryDto
    {
        public decimal TotalInventoryValue { get; set; }
        public decimal RawMaterialsValue { get; set; }
        public decimal WipValue { get; set; }
        public decimal FinishedGoodsValue { get; set; }
        public int TotalStockRecords { get; set; }
        public int LowStockCount { get; set; }
        public int OutOfStockCount { get; set; }
        public int ExpiringLotsCount { get; set; }
    }

    public class StockBalanceDetailedDto
    {
        public long Id { get; set; }
        public long ItemId { get; set; }
        public string ItemCode { get; set; } = string.Empty;
        public string ItemName { get; set; } = string.Empty;
        public string ItemType { get; set; } = string.Empty;
        public string Unit { get; set; } = "كجم";
        public long WarehouseId { get; set; }
        public string WarehouseName { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public decimal ReservedQuantity { get; set; }
        public decimal AvailableQuantity { get; set; }
        public decimal EstimatedUnitCost { get; set; }
        public decimal TotalValue { get; set; }
        public DateTime LastUpdatedAt { get; set; }
    }

    public class StockBalanceImportDto
    {
        public long ItemId { get; set; }
        public long WarehouseId { get; set; }
        public decimal Quantity { get; set; }
        public string? Notes { get; set; }
    }

    public class StockMovementDto
    {
        public long Id { get; set; }
        public DateTime MovementDate { get; set; }
        public string MovementType { get; set; } = string.Empty;
        public string WarehouseName { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public decimal UnitCost { get; set; }
        public string? ReferenceType { get; set; } = string.Empty;
        public long? ReferenceId { get; set; }
        public string? Notes { get; set; }
    }
}