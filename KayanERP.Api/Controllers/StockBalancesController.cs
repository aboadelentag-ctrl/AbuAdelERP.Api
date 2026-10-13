using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using KayanERP.Api.Models;

namespace KayanERP.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class StockBalancesController : ControllerBase
    {
        private readonly AppDbContext _context;

        public StockBalancesController(AppDbContext context)
        {
            _context = context;
        }

        // ==============================================================
        // 1. القوائم المرجعية لشاشة الأرصدة (الفروع والمستودعات النشطة)
        // ==============================================================
        [HttpGet("lookups")]
        public async Task<IActionResult> GetLookups()
        {
            try
            {
                var branches = await _context.Branches
                    .AsNoTracking()
                    .Where(b => b.Active)
                    .OrderBy(b => b.BranchName)
                    .Select(b => new
                    {
                        b.Id,
                        b.BranchCode,
                        b.BranchName
                    })
                    .ToListAsync();

                var warehouses = await _context.Warehouses
                    .AsNoTracking()
                    .Where(w => w.Active)
                    .OrderBy(w => w.WarehouseName)
                    .Select(w => new
                    {
                        w.Id,
                        w.BranchId,
                        w.WarehouseCode,
                        w.WarehouseName
                    })
                    .ToListAsync();

                return Ok(new { branches, warehouses });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"خطأ في استرجاع القوائم المرجعية: {ex.GetBaseException().Message}");
            }
        }

        // ==============================================================
        // 2. جلب الأرصدة اللحظية مع الفلاتر المتقدمة (الفرع، المستودع، النواقص)
        // ==============================================================
        [HttpGet]
        public async Task<IActionResult> GetBalances(
            [FromQuery] long? branchId = null,
            [FromQuery] long? warehouseId = null,
            [FromQuery] long? categoryId = null,
            [FromQuery] string? search = null,
            [FromQuery] bool lowStockOnly = false)
        {
            try
            {
                var query = _context.StockBalances
                    .Include(s => s.Item)
                        .ThenInclude(i => i.ItemCategory)
                    .Include(s => s.Warehouse)
                        .ThenInclude(w => w.Branch)
                    .AsNoTracking();

                if (branchId.HasValue && branchId.Value > 0)
                {
                    query = query.Where(s => s.Warehouse.BranchId == branchId.Value);
                }

                if (warehouseId.HasValue && warehouseId.Value > 0)
                {
                    query = query.Where(s => s.WarehouseId == warehouseId.Value);
                }

                if (categoryId.HasValue && categoryId.Value > 0)
                {
                    query = query.Where(s => s.Item.CategoryId == categoryId.Value);
                }

                if (!string.IsNullOrWhiteSpace(search))
                {
                    var term = search.Trim().ToLower();
                    query = query.Where(s => s.Item.ItemName.ToLower().Contains(term)
                                          || s.Item.ItemCode.ToLower().Contains(term)
                                          || (s.Item.Barcode != null && s.Item.Barcode.ToLower().Contains(term)));
                }

                // فلترة النواقص المحصنة: تشمل الرصيد المنتهي تماماً أو الذي كسر حد الطلب أو الأمان
                if (lowStockOnly)
                {
                    query = query.Where(s => s.Quantity <= 0
                                          || (s.Item.ReorderLevel > 0 && s.Quantity <= s.Item.ReorderLevel)
                                          || (s.Item.SafetyStock > 0 && s.Quantity <= s.Item.SafetyStock));
                }

                var balances = await query
                    .OrderBy(s => s.Warehouse.WarehouseName)
                    .ThenBy(s => s.Item.ItemName)
                    .Select(s => new StockBalanceViewDto
                    {
                        Id = s.Id,
                        WarehouseId = s.WarehouseId,
                        WarehouseCode = s.Warehouse.WarehouseCode,
                        WarehouseName = s.Warehouse.WarehouseName,
                        BranchId = s.Warehouse.BranchId,
                        BranchName = s.Warehouse.Branch != null ? s.Warehouse.Branch.BranchName : "الفرع الرئيسي",
                        ItemId = s.ItemId,
                        ItemCode = s.Item.ItemCode,
                        ItemName = s.Item.ItemName,
                        CategoryName = s.Item.ItemCategory != null ? s.Item.ItemCategory.CategoryName : "عام",
                        Unit = s.Item.Unit ?? "قطعة",
                        Quantity = s.Quantity,
                        ReservedQuantity = s.ReservedQuantity,
                        AvailableQuantity = s.Quantity - s.ReservedQuantity,
                        ReorderLevel = s.Item.ReorderLevel,
                        SafetyStock = s.Item.SafetyStock,
                        BasePrice = s.Item.BasePrice,
                        TotalStockValue = s.Quantity * s.Item.BasePrice,
                        LastUpdatedAt = s.LastUpdatedAt,
                        Status = s.Quantity <= 0 ? "نفد الرصيد"
                               : (s.Item.SafetyStock > 0 && s.Quantity <= s.Item.SafetyStock) ? "حرج (تحت حد الأمان)"
                               : (s.Item.ReorderLevel > 0 && s.Quantity <= s.Item.ReorderLevel) ? "حد الطلب"
                               : "متوفر"
                    })
                    .ToListAsync();

                return Ok(balances);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"خطأ في استرجاع الأرصدة المخزنية: {ex.GetBaseException().Message}");
            }
        }

        // ==============================================================
        // 3. مؤشرات ملخص المخزون (Inventory KPI Dashboard)
        // ==============================================================
        [HttpGet("kpis")]
        public async Task<IActionResult> GetStockKpis([FromQuery] long? branchId = null, [FromQuery] long? warehouseId = null)
        {
            try
            {
                var query = _context.StockBalances
                    .Include(s => s.Item)
                    .Include(s => s.Warehouse)
                    .AsNoTracking();

                if (branchId.HasValue && branchId.Value > 0)
                    query = query.Where(s => s.Warehouse.BranchId == branchId.Value);

                if (warehouseId.HasValue && warehouseId.Value > 0)
                    query = query.Where(s => s.WarehouseId == warehouseId.Value);

                var items = await query.Select(s => new
                {
                    s.Quantity,
                    Available = s.Quantity - s.ReservedQuantity,
                    Price = s.Item.BasePrice,
                    Reorder = s.Item.ReorderLevel,
                    Safety = s.Item.SafetyStock
                }).ToListAsync();

                var kpi = new StockKpiSummaryDto
                {
                    TotalItemsCount = items.Count,
                    TotalStockQuantity = items.Sum(i => i.Quantity),
                    TotalAvailableQuantity = items.Sum(i => i.Available),
                    EstimatedStockValue = items.Sum(i => i.Quantity * i.Price),
                    LowStockCount = items.Count(i => i.Quantity > 0 && ((i.Reorder > 0 && i.Quantity <= i.Reorder) || (i.Safety > 0 && i.Quantity <= i.Safety))),
                    OutOfStockCount = items.Count(i => i.Quantity <= 0)
                };

                return Ok(kpi);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"خطأ في حساب المؤشرات: {ex.GetBaseException().Message}");
            }
        }
    }

    // ==============================================================
    // DTOs الخاصة بالأرصدة
    // ==============================================================
    public class StockBalanceViewDto
    {
        public long Id { get; set; }
        public long WarehouseId { get; set; }
        public string WarehouseCode { get; set; } = string.Empty;
        public string WarehouseName { get; set; } = string.Empty;
        public long? BranchId { get; set; }
        public string BranchName { get; set; } = string.Empty;
        public long ItemId { get; set; }
        public string ItemCode { get; set; } = string.Empty;
        public string ItemName { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public string Unit { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public decimal ReservedQuantity { get; set; }
        public decimal AvailableQuantity { get; set; }
        public decimal ReorderLevel { get; set; }
        public decimal SafetyStock { get; set; }
        public decimal BasePrice { get; set; }
        public decimal TotalStockValue { get; set; }
        public DateTime? LastUpdatedAt { get; set; }
        public string Status { get; set; } = "متوفر";
    }

    public class StockKpiSummaryDto
    {
        public int TotalItemsCount { get; set; }
        public decimal TotalStockQuantity { get; set; }
        public decimal TotalAvailableQuantity { get; set; }
        public decimal EstimatedStockValue { get; set; }
        public int LowStockCount { get; set; }
        public int OutOfStockCount { get; set; }
    }
}