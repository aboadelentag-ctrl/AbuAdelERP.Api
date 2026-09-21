using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AbuAdelERP.Api.Models;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace AbuAdelERP.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class WarehousesController : ControllerBase
    {
        private readonly AppDbContext _context;

        public WarehousesController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetWarehouses()
        {
            var warehouses = await _context.Warehouses
                .Include(w => w.ParentWarehouse)
                .Select(w => new WarehouseDto
                {
                    Id = w.Id,
                    WarehouseCode = w.WarehouseCode,
                    WarehouseName = w.WarehouseName,
                    WarehouseType = w.WarehouseType,
                    ParentWarehouseId = w.ParentWarehouseId,
                    ParentWarehouseName = w.ParentWarehouse != null ? w.ParentWarehouse.WarehouseName : null,
                    Description = w.Description,
                    Active = w.Active
                })
                .ToListAsync();

            return Ok(warehouses);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetWarehouse(long id)
        {
            var warehouse = await _context.Warehouses
                .Include(w => w.ParentWarehouse)
                .Where(w => w.Id == id)
                .Select(w => new WarehouseDto
                {
                    Id = w.Id,
                    WarehouseCode = w.WarehouseCode,
                    WarehouseName = w.WarehouseName,
                    WarehouseType = w.WarehouseType,
                    ParentWarehouseId = w.ParentWarehouseId,
                    ParentWarehouseName = w.ParentWarehouse != null ? w.ParentWarehouse.WarehouseName : null,
                    Description = w.Description,
                    Active = w.Active
                })
                .FirstOrDefaultAsync();

            if (warehouse == null) return NotFound();
            return Ok(warehouse);
        }

        [HttpPost]
        public async Task<IActionResult> PostWarehouse([FromBody] WarehouseDto dto)
        {
            var company = await _context.Companies.FirstOrDefaultAsync();
            if (company == null) return BadRequest("لا توجد شركات مسجلة بالنظام.");

            dto.WarehouseCode = dto.WarehouseCode?.Trim() ?? string.Empty;

            bool exists = await _context.Warehouses
                .AnyAsync(w => w.CompanyId == company.Id && w.WarehouseCode.ToLower() == dto.WarehouseCode.ToLower());

            if (exists) return BadRequest($"كود المخزن ({dto.WarehouseCode}) مستخدم بالفعل.");

            var warehouse = new Warehouse
            {
                CompanyId = company.Id,
                WarehouseCode = dto.WarehouseCode,
                WarehouseName = dto.WarehouseName?.Trim() ?? string.Empty,
                WarehouseType = dto.WarehouseType?.Trim() ?? string.Empty,
                ParentWarehouseId = dto.ParentWarehouseId > 0 ? dto.ParentWarehouseId : null,
                Description = dto.Description?.Trim() ?? string.Empty,
                Active = dto.Active,
                CreatedAt = DateTime.UtcNow
            };

            _context.Warehouses.Add(warehouse);
            await _context.SaveChangesAsync();

            dto.Id = warehouse.Id;
            return Ok(dto);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> PutWarehouse(long id, [FromBody] WarehouseDto dto)
        {
            if (id != dto.Id) return BadRequest("رقم المخزن غير متطابق.");
            if (dto.ParentWarehouseId == id) return BadRequest("لا يمكن جعل المخزن تابعاً لنفسه.");

            var existing = await _context.Warehouses.FindAsync(id);
            if (existing == null) return NotFound();

            dto.WarehouseCode = dto.WarehouseCode?.Trim() ?? string.Empty;

            bool exists = await _context.Warehouses
                .AnyAsync(w => w.CompanyId == existing.CompanyId
                            && w.WarehouseCode.ToLower() == dto.WarehouseCode.ToLower()
                            && w.Id != id);

            if (exists) return BadRequest($"كود المخزن ({dto.WarehouseCode}) مستخدم في مخزن آخر.");

            existing.WarehouseCode = dto.WarehouseCode;
            existing.WarehouseName = dto.WarehouseName?.Trim() ?? string.Empty;
            existing.WarehouseType = dto.WarehouseType?.Trim() ?? string.Empty;
            existing.ParentWarehouseId = dto.ParentWarehouseId > 0 ? dto.ParentWarehouseId : null;
            existing.Description = dto.Description?.Trim() ?? string.Empty;
            existing.Active = dto.Active;
            existing.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteWarehouse(long id)
        {
            var warehouse = await _context.Warehouses.FindAsync(id);
            if (warehouse == null) return NotFound();

            bool hasChildren = await _context.Warehouses.AnyAsync(w => w.ParentWarehouseId == id);
            if (hasChildren)
            {
                return BadRequest("لا يمكن حذف هذا المخزن لأنه يمتلك مخازن فرعية تابعة له.");
            }

            bool hasStock = await _context.StockBalances.AnyAsync(s => s.WarehouseId == id && s.Quantity > 0);
            if (hasStock)
            {
                return BadRequest("لا يمكن حذف هذا المخزن لأنه يحتوي على أرصدة أصناف فعلية.");
            }

            _context.Warehouses.Remove(warehouse);
            await _context.SaveChangesAsync();
            return NoContent();
        }

        // جلب رصيد المخزن مع تجميع الفروع التابعة (Roll-up)
        [HttpGet("{id}/stock-balance")]
        public async Task<IActionResult> GetWarehouseStockBalance(long id)
        {
            var warehouseIds = new List<long> { id };
            var subWarehouseIds = await _context.Warehouses
                .Where(w => w.ParentWarehouseId == id)
                .Select(w => w.Id)
                .ToListAsync();

            warehouseIds.AddRange(subWarehouseIds);

            var aggregatedStock = await _context.StockBalances
                .Where(s => warehouseIds.Contains(s.WarehouseId))
                .Include(s => s.Item)
                .GroupBy(s => new { s.ItemId, s.Item.ItemCode, s.Item.ItemName })
                .Select(g => new
                {
                    g.Key.ItemId,
                    g.Key.ItemCode,
                    g.Key.ItemName,
                    TotalQuantity = g.Sum(s => s.Quantity),
                    TotalReserved = g.Sum(s => s.ReservedQuantity),
                    TotalAvailable = g.Sum(s => s.AvailableQuantity ?? (s.Quantity - s.ReservedQuantity))
                })
                .ToListAsync();

            return Ok(aggregatedStock);
        }
    }

    public class WarehouseDto
    {
        public long Id { get; set; }
        public string WarehouseCode { get; set; } = string.Empty;
        public string WarehouseName { get; set; } = string.Empty;
        public string WarehouseType { get; set; } = string.Empty;
        public long? ParentWarehouseId { get; set; }
        public string? ParentWarehouseName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool Active { get; set; } = true;
    }
}