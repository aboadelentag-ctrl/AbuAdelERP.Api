using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AbuAdelERP.Api.Models;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace AbuAdelERP.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class StockBalancesController : ControllerBase
    {
        private readonly AppDbContext _context;

        public StockBalancesController(AppDbContext context)
        {
            _context = context;
        }

        // جلب الأرصدة مع دعم تجميع الشجرة (Roll-up) لو تم اختيار مخزن رئيسي
        [HttpGet]
        public async Task<IActionResult> GetStockBalances([FromQuery] long? warehouseId, [FromQuery] long? itemId)
        {
            var query = _context.StockBalances
                .Include(s => s.Item)
                .Include(s => s.Warehouse)
                .AsQueryable();

            if (warehouseId.HasValue && warehouseId.Value > 0)
            {
                // جلب المخزن المختار + أي مخازن فرعية تابعة له تحت نفس الشجرة
                var warehouseIds = new List<long> { warehouseId.Value };
                var subIds = await _context.Warehouses
                    .Where(w => w.ParentWarehouseId == warehouseId.Value)
                    .Select(w => w.Id)
                    .ToListAsync();

                warehouseIds.AddRange(subIds);
                query = query.Where(s => warehouseIds.Contains(s.WarehouseId));
            }

            if (itemId.HasValue && itemId.Value > 0)
            {
                query = query.Where(s => s.ItemId == itemId.Value);
            }

            // تجميع النتيجة لعرض الصنف مع إجمالي الكميات لو شملت أكثر من مخزن فرعي
            var result = await query
                .GroupBy(s => new { s.ItemId, s.Item.ItemCode, s.Item.ItemName })
                .Select(g => new StockBalanceViewDto
                {
                    ItemId = g.Key.ItemId,
                    ItemCode = g.Key.ItemCode,
                    ItemName = g.Key.ItemName,
                    TotalQuantity = g.Sum(s => s.Quantity),
                    TotalReserved = g.Sum(s => s.ReservedQuantity),
                    TotalAvailable = g.Sum(s => s.AvailableQuantity ?? (s.Quantity - s.ReservedQuantity))
                })
                .ToListAsync();

            return Ok(result);
        }
    }

    public class StockBalanceViewDto
    {
        public long ItemId { get; set; }
        public string ItemCode { get; set; } = string.Empty;
        public string ItemName { get; set; } = string.Empty;
        public decimal TotalQuantity { get; set; }
        public decimal TotalReserved { get; set; }
        public decimal TotalAvailable { get; set; }
    }
}