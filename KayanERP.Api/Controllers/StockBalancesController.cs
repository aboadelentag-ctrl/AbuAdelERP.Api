using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using KayanERP.Api.DTOs;
using KayanERP.Api.Models;

namespace KayanERP.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class StockBalancesController : ControllerBase
    {
        private readonly AppDbContext _context;

        public StockBalancesController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet("lookups")]
        public async Task<ActionResult<StockLookupsDto>> GetLookups()
        {
            var branches = await _context.Branches
                .AsNoTracking()
                .Where(b => b.Active)
                .OrderBy(b => b.BranchName)
                .Select(b => new BranchForBalanceDto
                {
                    Id = b.Id,
                    BranchCode = b.BranchCode,
                    BranchName = b.BranchName
                })
                .ToListAsync();

            var warehouses = await _context.Warehouses
                .AsNoTracking()
                .Where(w => w.Active)
                .OrderBy(w => w.WarehouseName)
                .Select(w => new WarehouseForBalanceDto
                {
                    Id = w.Id,
                    BranchId = w.BranchId,
                    WarehouseCode = w.WarehouseCode,
                    WarehouseName = w.WarehouseName
                })
                .ToListAsync();

            return Ok(new StockLookupsDto
            {
                Branches = branches,
                Warehouses = warehouses
            });
        }

        [HttpGet]
        public async Task<ActionResult<List<StockBalanceListItemDto>>> GetBalances(
            [FromQuery] long? branchId = null,
            [FromQuery] long? warehouseId = null,
            [FromQuery] long? categoryId = null,
            [FromQuery] string? search = null,
            [FromQuery] bool lowStockOnly = false)
        {
            var query = _context.StockBalances
                .Include(s => s.Item).ThenInclude(i => i.ItemCategory)
                .Include(s => s.Warehouse).ThenInclude(w => w.Branch)
                .AsNoTracking()
                .AsQueryable();

            if (branchId.HasValue && branchId.Value > 0)
                query = query.Where(s => s.Warehouse.BranchId == branchId.Value);

            if (warehouseId.HasValue && warehouseId.Value > 0)
                query = query.Where(s => s.WarehouseId == warehouseId.Value);

            if (categoryId.HasValue && categoryId.Value > 0)
                query = query.Where(s => s.Item.CategoryId == categoryId.Value);

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim().ToLower();
                query = query.Where(s =>
                    s.Item.ItemName.ToLower().Contains(term) ||
                    s.Item.ItemCode.ToLower().Contains(term) ||
                    (s.Item.Barcode != null && s.Item.Barcode.ToLower().Contains(term)));
            }

            if (lowStockOnly)
            {
                query = query.Where(s =>
                    s.Quantity <= 0 ||
                    (s.Item.ReorderLevel > 0 && s.Quantity <= s.Item.ReorderLevel) ||
                    (s.Item.SafetyStock > 0 && s.Quantity <= s.Item.SafetyStock));
            }

            var balances = await query
                .OrderBy(s => s.Warehouse.WarehouseName)
                .ThenBy(s => s.Item.ItemName)
                .Select(s => new StockBalanceListItemDto
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

        [HttpGet("kpis")]
        public async Task<ActionResult<StockKpiSummaryDto>> GetStockKpis(
            [FromQuery] long? branchId = null,
            [FromQuery] long? warehouseId = null)
        {
            var query = _context.StockBalances
                .Include(s => s.Item)
                .Include(s => s.Warehouse)
                .AsNoTracking()
                .AsQueryable();

            if (branchId.HasValue && branchId.Value > 0)
                query = query.Where(s => s.Warehouse.BranchId == branchId.Value);

            if (warehouseId.HasValue && warehouseId.Value > 0)
                query = query.Where(s => s.WarehouseId == warehouseId.Value);

            var items = await query
                .Select(s => new
                {
                    s.Quantity,
                    Available = s.Quantity - s.ReservedQuantity,
                    Price = s.Item.BasePrice,
                    Reorder = s.Item.ReorderLevel,
                    Safety = s.Item.SafetyStock
                })
                .ToListAsync();

            var kpi = new StockKpiSummaryDto
            {
                TotalItemsCount = items.Count,
                TotalStockQuantity = items.Sum(i => i.Quantity),
                TotalAvailableQuantity = items.Sum(i => i.Available),
                EstimatedStockValue = items.Sum(i => i.Quantity * i.Price),
                LowStockCount = items.Count(i =>
                    i.Quantity > 0 &&
                    ((i.Reorder > 0 && i.Quantity <= i.Reorder) ||
                     (i.Safety > 0 && i.Quantity <= i.Safety))),
                OutOfStockCount = items.Count(i => i.Quantity <= 0)
            };

            return Ok(kpi);
        }
    }
}