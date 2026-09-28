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

        // ==============================================================
        // 1. جلب كافة المخازن مع بيانات المخزن الأب
        // ==============================================================
        [HttpGet]
        public async Task<IActionResult> GetWarehouses()
        {
            try
            {
                var warehouses = await _context.Warehouses
                    .Include(w => w.ParentWarehouse)
                    .AsNoTracking()
                    .OrderBy(w => w.WarehouseCode)
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
            catch (Exception ex)
            {
                return StatusCode(500, $"خطأ في جلب بيانات المخازن: {ex.GetBaseException().Message}");
            }
        }

        // ==============================================================
        // 2. جلب مخزن محدد
        // ==============================================================
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

            if (warehouse == null) return NotFound("المخزن غير موجود.");
            return Ok(warehouse);
        }

        // ==============================================================
        // 3. إضافة مخزن جديد
        // ==============================================================
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

        // ==============================================================
        // 4. تعديل بيانات مخزن
        // ==============================================================
        [HttpPut("{id}")]
        public async Task<IActionResult> PutWarehouse(long id, [FromBody] WarehouseDto dto)
        {
            if (id != dto.Id) return BadRequest("رقم المخزن غير متطابق.");
            if (dto.ParentWarehouseId == id) return BadRequest("لا يمكن جعل المخزن تابعاً لنفسه.");

            var existing = await _context.Warehouses.FindAsync(id);
            if (existing == null) return NotFound("المخزن غير موجود.");

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

        // ==============================================================
        // 5. حذف مخزن
        // ==============================================================
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteWarehouse(long id)
        {
            var warehouse = await _context.Warehouses.FindAsync(id);
            if (warehouse == null) return NotFound("المخزن غير موجود.");

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

        // ==============================================================
        // 6. استيراد وتحديث المخازن دفعة واحدة من الإكسيل (Bulk Import)
        // ==============================================================
        [HttpPost("bulk-import")]
        public async Task<IActionResult> BulkImportWarehouses([FromBody] List<WarehouseImportDto> items)
        {
            if (items == null || !items.Any())
                return BadRequest("لا توجد بيانات للاستيراد.");

            var company = await _context.Companies.FirstOrDefaultAsync();
            if (company == null) return BadRequest("لا توجد شركات مسجلة بالنظام.");

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                int created = 0;
                int updated = 0;
                var now = DateTime.UtcNow;

                // المرحلة الأولى: إنشاء أو تحديث البيانات الأساسية للمخزن
                foreach (var item in items)
                {
                    var code = item.WarehouseCode?.Trim() ?? string.Empty;
                    if (string.IsNullOrEmpty(code)) continue;

                    var existing = await _context.Warehouses
                        .FirstOrDefaultAsync(w => w.CompanyId == company.Id && w.WarehouseCode.ToLower() == code.ToLower());

                    if (existing != null)
                    {
                        existing.WarehouseName = !string.IsNullOrWhiteSpace(item.WarehouseName) ? item.WarehouseName.Trim() : existing.WarehouseName;
                        existing.WarehouseType = !string.IsNullOrWhiteSpace(item.WarehouseType) ? item.WarehouseType.Trim() : existing.WarehouseType;
                        existing.Description = item.Description?.Trim() ?? existing.Description;
                        existing.Active = item.Active;
                        existing.UpdatedAt = now;
                        updated++;
                    }
                    else
                    {
                        var newWarehouse = new Warehouse
                        {
                            CompanyId = company.Id,
                            WarehouseCode = code,
                            WarehouseName = !string.IsNullOrWhiteSpace(item.WarehouseName) ? item.WarehouseName.Trim() : code,
                            WarehouseType = !string.IsNullOrWhiteSpace(item.WarehouseType) ? item.WarehouseType.Trim() : "خامات",
                            Description = item.Description?.Trim() ?? string.Empty,
                            Active = item.Active,
                            CreatedAt = now
                        };
                        _context.Warehouses.Add(newWarehouse);
                        created++;
                    }
                }

                await _context.SaveChangesAsync();

                // المرحلة الثانية: ربط العلاقات الشجرية (المخزن الأب) بعد ضمان وجود كافة المعرفات في قاعدة البيانات
                var allWarehouses = await _context.Warehouses.Where(w => w.CompanyId == company.Id).ToListAsync();

                foreach (var item in items)
                {
                    var code = item.WarehouseCode?.Trim() ?? string.Empty;
                    var current = allWarehouses.FirstOrDefault(w => w.WarehouseCode.Equals(code, StringComparison.OrdinalIgnoreCase));
                    if (current == null) continue;

                    if (!string.IsNullOrWhiteSpace(item.ParentWarehouseCodeOrName))
                    {
                        var parentTerm = item.ParentWarehouseCodeOrName.Trim();
                        var parent = allWarehouses.FirstOrDefault(w =>
                            w.WarehouseCode.Equals(parentTerm, StringComparison.OrdinalIgnoreCase) ||
                            w.WarehouseName.Equals(parentTerm, StringComparison.OrdinalIgnoreCase));

                        if (parent != null && parent.Id != current.Id)
                        {
                            current.ParentWarehouseId = parent.Id;
                        }
                    }
                    else
                    {
                        // إذا تُركت خانة الأب فارغة، يعتبر مخزناً رئيسياً مستقلاً
                        current.ParentWarehouseId = null;
                    }
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Ok(new
                {
                    success = true,
                    createdCount = created,
                    updatedCount = updated,
                    message = $"تم استيراد البيانات بنجاح: تم إنشاء ({created}) مخزن جديد، وتحديث بيانات ({updated}) مخزن مسجل مسبقاً."
                });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, $"خطأ أثناء استيراد المخازن: {ex.GetBaseException().Message}");
            }
        }

        // ==============================================================
        // 7. جلب رصيد المخزن مع تجميع الفروع التابعة (Roll-up)
        // ==============================================================
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

    public class WarehouseImportDto
    {
        public string WarehouseCode { get; set; } = string.Empty;
        public string WarehouseName { get; set; } = string.Empty;
        public string WarehouseType { get; set; } = "خامات";
        public string? ParentWarehouseCodeOrName { get; set; }
        public string? Description { get; set; }
        public bool Active { get; set; } = true;
    }
}