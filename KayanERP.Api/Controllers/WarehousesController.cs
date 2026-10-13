using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using KayanERP.Api.Models;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace KayanERP.Api.Controllers
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
        // 1. جلب قائمة الفروع النشطة لمستودعات النظام
        // ==============================================================
        [HttpGet("branches")]
        public async Task<IActionResult> GetBranches()
        {
            try
            {
                var branches = await _context.Branches
                    .AsNoTracking()
                    .Where(b => b.Active)
                    .OrderBy(b => b.BranchName)
                    .Select(b => new WarehouseBranchLookupDto
                    {
                        Id = b.Id,
                        BranchCode = b.BranchCode,
                        BranchName = b.BranchName
                    })
                    .ToListAsync();

                if (!branches.Any())
                {
                    branches.Add(new WarehouseBranchLookupDto
                    {
                        Id = 1L,
                        BranchCode = "MAIN",
                        BranchName = "الفرع الرئيسي - المصنع"
                    });
                }

                return Ok(branches);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"خطأ في جلب الفروع: {ex.GetBaseException().Message}");
            }
        }

        // ==============================================================
        // 2. جلب كافة المخازن (مع دعم الفلترة بالفرع والشجرة الهرمية)
        // ==============================================================
        [HttpGet]
        public async Task<IActionResult> GetWarehouses([FromQuery] long? branchId = null)
        {
            try
            {
                var query = _context.Warehouses
                    .Include(w => w.ParentWarehouse)
                    .Include(w => w.Branch)
                    .AsNoTracking();

                if (branchId.HasValue && branchId.Value > 0)
                {
                    query = query.Where(w => w.BranchId == branchId.Value);
                }

                var warehouses = await query
                    .OrderBy(w => w.BranchId)
                    .ThenBy(w => w.WarehouseCode)
                    .Select(w => new WarehouseDto
                    {
                        Id = w.Id,
                        BranchId = w.BranchId,
                        BranchName = w.Branch != null ? w.Branch.BranchName : "الفرع الرئيسي",
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
        // 3. جلب مخزن محدد
        // ==============================================================
        [HttpGet("{id:long}")]
        public async Task<IActionResult> GetWarehouse(long id)
        {
            try
            {
                var warehouse = await _context.Warehouses
                    .Include(w => w.ParentWarehouse)
                    .Include(w => w.Branch)
                    .AsNoTracking()
                    .Where(w => w.Id == id)
                    .Select(w => new WarehouseDto
                    {
                        Id = w.Id,
                        BranchId = w.BranchId,
                        BranchName = w.Branch != null ? w.Branch.BranchName : "الفرع الرئيسي",
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
            catch (Exception ex)
            {
                return StatusCode(500, $"خطأ في استعلام المخزن: {ex.GetBaseException().Message}");
            }
        }

        // ==============================================================
        // 4. إضافة مخزن جديد (مربوط بالفرع)
        // ==============================================================
        [HttpPost]
        public async Task<IActionResult> PostWarehouse([FromBody] WarehouseDto dto)
        {
            if (dto == null) return BadRequest("بيانات غير صالحة.");

            dto.WarehouseCode = dto.WarehouseCode?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(dto.WarehouseCode))
                return BadRequest("كود المستودع إجباري.");

            var company = await _context.Companies.FirstOrDefaultAsync();
            long companyId = company?.Id ?? 1;

            bool exists = await _context.Warehouses
                .AnyAsync(w => w.CompanyId == companyId && w.WarehouseCode.ToLower() == dto.WarehouseCode.ToLower());

            if (exists) return BadRequest($"كود المستودع ({dto.WarehouseCode}) مستخدم بالفعل.");

            long branchId = dto.BranchId.HasValue && dto.BranchId.Value > 0
                ? dto.BranchId.Value
                : (await _context.Branches.Select(b => b.Id).FirstOrDefaultAsync());

            if (branchId <= 0) branchId = 1;

            var warehouse = new Warehouse
            {
                CompanyId = companyId,
                BranchId = branchId,
                WarehouseCode = dto.WarehouseCode,
                WarehouseName = dto.WarehouseName?.Trim() ?? string.Empty,
                WarehouseType = dto.WarehouseType?.Trim() ?? "خامات",
                ParentWarehouseId = dto.ParentWarehouseId > 0 ? dto.ParentWarehouseId : null,
                Description = dto.Description?.Trim() ?? string.Empty,
                Active = dto.Active,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Warehouses.Add(warehouse);
            await _context.SaveChangesAsync();

            dto.Id = warehouse.Id;
            dto.BranchId = warehouse.BranchId;
            return Ok(dto);
        }

        // ==============================================================
        // 5. تعديل بيانات مخزن
        // ==============================================================
        [HttpPut("{id:long}")]
        public async Task<IActionResult> PutWarehouse(long id, [FromBody] WarehouseDto dto)
        {
            if (dto == null || id != dto.Id) return BadRequest("بيانات المستودع غير متطابقة.");
            if (dto.ParentWarehouseId == id) return BadRequest("لا يمكن جعل المستودع تابعاً لنفسه.");

            var existing = await _context.Warehouses.FindAsync(id);
            if (existing == null) return NotFound("المستودع غير موجود.");

            dto.WarehouseCode = dto.WarehouseCode?.Trim() ?? string.Empty;

            bool exists = await _context.Warehouses
                .AnyAsync(w => w.CompanyId == existing.CompanyId
                            && w.WarehouseCode.ToLower() == dto.WarehouseCode.ToLower()
                            && w.Id != id);

            if (exists) return BadRequest($"كود المستودع ({dto.WarehouseCode}) مستخدم في مستودع آخر.");

            if (dto.BranchId.HasValue && dto.BranchId.Value > 0)
            {
                existing.BranchId = dto.BranchId.Value;
            }

            existing.WarehouseCode = dto.WarehouseCode;
            existing.WarehouseName = dto.WarehouseName?.Trim() ?? string.Empty;
            existing.WarehouseType = dto.WarehouseType?.Trim() ?? string.Empty;
            existing.ParentWarehouseId = dto.ParentWarehouseId > 0 ? dto.ParentWarehouseId : null;
            existing.Description = dto.Description?.Trim() ?? string.Empty;
            existing.Active = dto.Active;
            existing.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return Ok(existing);
        }

        // ==============================================================
        // 6. حذف مستودع (محصن ضد كسر القيود المحاسبية والتاريخية)
        // ==============================================================
        [HttpDelete("{id:long}")]
        public async Task<IActionResult> DeleteWarehouse(long id)
        {
            try
            {
                var warehouse = await _context.Warehouses.FindAsync(id);
                if (warehouse == null) return NotFound("المستودع غير موجود.");

                bool hasChildren = await _context.Warehouses.AnyAsync(w => w.ParentWarehouseId == id);
                if (hasChildren)
                {
                    return BadRequest("لا يمكن حذف هذا المستودع لأنه يمتلك مستودعات فرعية تابعة له في الشجرة الهرمية.");
                }

                bool hasStock = await _context.StockBalances.AnyAsync(s => s.WarehouseId == id && s.Quantity > 0);
                if (hasStock)
                {
                    return BadRequest("لا يمكن حذف هذا المستودع لأنه يحتوي على أرصدة أصناف فعلية غير صفرية.");
                }

                bool hasMovements = await _context.StockMovements.AnyAsync(m => m.WarehouseId == id);
                if (hasMovements)
                {
                    warehouse.Active = false;
                    warehouse.UpdatedAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync();
                    return Ok(new { success = true, message = "تم تعطيل المستودع وإيقافه بدلاً من حذفه لوجود حركات وسجلات مخزنية تاريخية مرتبطة به." });
                }

                _context.Warehouses.Remove(warehouse);
                await _context.SaveChangesAsync();
                return Ok(new { success = true, message = "تم حذف المستودع بنجاح." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"خطأ أثناء معالجة الحذف: {ex.GetBaseException().Message}");
            }
        }

        // ==============================================================
        // 7. استيراد وتحديث المخازن دفعة واحدة (Bulk Import مع الفروع)
        // ==============================================================
        [HttpPost("bulk-import")]
        public async Task<IActionResult> BulkImportWarehouses([FromBody] List<WarehouseImportDto> items)
        {
            if (items == null || !items.Any())
                return BadRequest("لا توجد بيانات للاستيراد.");

            var company = await _context.Companies.FirstOrDefaultAsync();
            long companyId = company?.Id ?? 1;

            var defaultBranch = await _context.Branches.FirstOrDefaultAsync();
            long defaultBranchId = defaultBranch?.Id ?? 1;

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                int created = 0;
                int updated = 0;
                var now = DateTime.UtcNow;

                var branches = await _context.Branches.AsNoTracking().ToListAsync();

                foreach (var item in items)
                {
                    var code = item.WarehouseCode?.Trim() ?? string.Empty;
                    if (string.IsNullOrEmpty(code)) continue;

                    long targetBranchId = defaultBranchId;
                    if (!string.IsNullOrWhiteSpace(item.BranchCodeOrName))
                    {
                        var branchMatch = branches.FirstOrDefault(b =>
                            b.BranchCode.Equals(item.BranchCodeOrName.Trim(), StringComparison.OrdinalIgnoreCase) ||
                            b.BranchName.Equals(item.BranchCodeOrName.Trim(), StringComparison.OrdinalIgnoreCase));
                        if (branchMatch != null) targetBranchId = branchMatch.Id;
                    }
                    else if (item.BranchId.HasValue && item.BranchId.Value > 0)
                    {
                        targetBranchId = item.BranchId.Value;
                    }

                    var existing = await _context.Warehouses
                        .FirstOrDefaultAsync(w => w.CompanyId == companyId && w.WarehouseCode.ToLower() == code.ToLower());

                    if (existing != null)
                    {
                        existing.BranchId = targetBranchId;
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
                            CompanyId = companyId,
                            BranchId = targetBranchId,
                            WarehouseCode = code,
                            WarehouseName = !string.IsNullOrWhiteSpace(item.WarehouseName) ? item.WarehouseName.Trim() : code,
                            WarehouseType = !string.IsNullOrWhiteSpace(item.WarehouseType) ? item.WarehouseType.Trim() : "خامات",
                            Description = item.Description?.Trim() ?? string.Empty,
                            Active = item.Active,
                            CreatedAt = now,
                            UpdatedAt = now
                        };
                        _context.Warehouses.Add(newWarehouse);
                        created++;
                    }
                }

                await _context.SaveChangesAsync();

                var allWarehouses = await _context.Warehouses.Where(w => w.CompanyId == companyId).ToListAsync();
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
                    message = $"تم استيراد المستودعات بنجاح: تم إنشاء ({created}) وتحديث ({updated}) مستودع."
                });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, $"خطأ أثناء استيراد المستودعات: {ex.GetBaseException().Message}");
            }
        }

        // ==============================================================
        // 8. جلب رصيد المستودع مجمعاً مع المستودعات التابعة هرمياً
        // ==============================================================
        [HttpGet("{id:long}/stock-balance")]
        public async Task<IActionResult> GetWarehouseStockBalance(long id)
        {
            try
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
            catch (Exception ex)
            {
                return StatusCode(500, $"خطأ في جلب رصيد المستودع: {ex.GetBaseException().Message}");
            }
        }
    }

    public class WarehouseBranchLookupDto
    {
        public long Id { get; set; }
        public string BranchCode { get; set; } = string.Empty;
        public string BranchName { get; set; } = string.Empty;
    }

    public class WarehouseDto
    {
        public long Id { get; set; }
        public long? BranchId { get; set; } = 1;
        public string? BranchName { get; set; } = string.Empty;
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
        public long? BranchId { get; set; }
        public string? BranchCodeOrName { get; set; }
        public string WarehouseCode { get; set; } = string.Empty;
        public string WarehouseName { get; set; } = string.Empty;
        public string WarehouseType { get; set; } = "خامات";
        public string? ParentWarehouseCodeOrName { get; set; }
        public string? Description { get; set; }
        public bool Active { get; set; } = true;
    }
}