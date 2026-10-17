using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using KayanERP.Api.DTOs;
using KayanERP.Api.Models;
using KayanERP.Api.Services;

namespace KayanERP.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ItemsController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ITenantContext _tenantContext;

        public ItemsController(AppDbContext context, ITenantContext tenantContext)
        {
            _context = context;
            _tenantContext = tenantContext;
        }

        // ==============================================================
        // 1. جلب الأصناف
        // ==============================================================
        [HttpGet]
        public async Task<ActionResult<List<ItemApiDto>>> GetItems([FromQuery] string? itemType = null)
        {
            var query = _context.Items
                .AsNoTracking()
                .Where(i => i.ParentItemId == null)
                .Include(i => i.Department)
                .Include(i => i.Variants)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(itemType))
                query = query.Where(i => i.ItemType == itemType);

            var items = await query
                .OrderBy(i => i.Id)
                .Select(i => new ItemApiDto
                {
                    Id = i.Id,
                    CompanyId = i.CompanyId,
                    CategoryId = i.CategoryId,
                    DepartmentId = i.DepartmentId,
                    DepartmentName = i.Department != null ? i.Department.DepartmentName : (i.DepartmentName ?? "-"),
                    ItemCode = i.ItemCode,
                    ItemName = i.ItemName,
                    ItemType = i.ItemType,
                    Unit = i.Unit,
                    Active = i.Active,
                    HasExpiryDate = i.HasExpiryDate,
                    ShelfLifeDays = i.ShelfLifeDays,
                    ReorderLevel = i.ReorderLevel,
                    SafetyStock = i.SafetyStock,
                    MaxStock = i.MaxStock,
                    BasePrice = i.BasePrice,
                    Barcode = i.Barcode,
                    HasVariants = i.IsVariant || i.Variants.Any(),
                    IsVariant = i.IsVariant,
                    IsMerged = i.IsMerged,
                    IsContainer = i.IsContainer,
                    MultiUom = i.MultiUom,
                    HideDetailPrint = i.HideDetailPrint,
                    ParentItemId = i.ParentItemId,
                    Variants = i.Variants.Select(v => new ItemVariantApiDto
                    {
                        Id = v.Id,
                        VariantCode = v.ItemCode,
                        VariantName = v.ItemName,
                        PriceOverride = v.BasePrice,
                        Barcode = v.Barcode
                    }).ToList()
                })
                .ToListAsync();

            return Ok(items);
        }

        // ==============================================================
        // 2. جلب صنف محدد
        // ==============================================================
        [HttpGet("{id:long}")]
        public async Task<ActionResult<ItemApiDto>> GetItem(long id)
        {
            var item = await _context.Items
                .AsNoTracking()
                .Include(i => i.Department)
                .Include(i => i.Variants)
                .FirstOrDefaultAsync(i => i.Id == id);

            if (item == null)
                return NotFound(new { message = "الصنف غير موجود." });

            return Ok(new ItemApiDto
            {
                Id = item.Id,
                CompanyId = item.CompanyId,
                CategoryId = item.CategoryId,
                DepartmentId = item.DepartmentId,
                DepartmentName = item.Department != null ? item.Department.DepartmentName : item.DepartmentName,
                ItemCode = item.ItemCode,
                ItemName = item.ItemName,
                ItemType = item.ItemType,
                Unit = item.Unit,
                Active = item.Active,
                HasExpiryDate = item.HasExpiryDate,
                ShelfLifeDays = item.ShelfLifeDays,
                ReorderLevel = item.ReorderLevel,
                SafetyStock = item.SafetyStock,
                MaxStock = item.MaxStock,
                BasePrice = item.BasePrice,
                Barcode = item.Barcode,
                HasVariants = item.IsVariant || item.Variants.Any(),
                IsVariant = item.IsVariant,
                IsMerged = item.IsMerged,
                IsContainer = item.IsContainer,
                MultiUom = item.MultiUom,
                HideDetailPrint = item.HideDetailPrint,
                ParentItemId = item.ParentItemId,
                Variants = item.Variants.Select(v => new ItemVariantApiDto
                {
                    Id = v.Id,
                    VariantCode = v.ItemCode,
                    VariantName = v.ItemName,
                    PriceOverride = v.BasePrice,
                    Barcode = v.Barcode
                }).ToList()
            });
        }

        // ==============================================================
        // 3. إضافة صنف جديد
        // ==============================================================
        [HttpPost]
        public async Task<ActionResult> PostItem([FromBody] ItemApiDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.ItemCode))
                return BadRequest(new { message = "كود الصنف مطلوب." });

            var companyId = _tenantContext.CompanyId;

            // فحص التكرار داخل نفس الشركة
            var exists = await _context.Items
                .AnyAsync(i => i.CompanyId == companyId &&
                               i.ItemCode.ToLower() == dto.ItemCode.Trim().ToLower());

            if (exists)
                return Conflict(new { message = $"كود الصنف [{dto.ItemCode.Trim()}] مسجل مسبقاً." });

            // تأكيد CategoryId من نفس الشركة
            var categoryId = dto.CategoryId > 0 ? dto.CategoryId : 1;
            var categoryExists = await _context.ItemCategories
                .AnyAsync(c => c.Id == categoryId && c.CompanyId == companyId);
            if (!categoryExists)
                return BadRequest(new { message = "التصنيف المحدد غير موجود." });

            // تأكيد DepartmentId
            long? targetDeptId = null;
            string? targetDeptName = null;
            if (dto.DepartmentId.HasValue && dto.DepartmentId.Value > 0)
            {
                var dept = await _context.Departments
                    .FirstOrDefaultAsync(d => d.Id == dto.DepartmentId.Value && d.CompanyId == companyId);
                if (dept != null)
                {
                    targetDeptId = dept.Id;
                    targetDeptName = dept.DepartmentName;
                }
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var item = new Item
                {
                    CompanyId = companyId,
                    CategoryId = categoryId,
                    DepartmentId = targetDeptId,
                    DepartmentName = targetDeptName,
                    ItemCode = dto.ItemCode.Trim(),
                    ItemName = dto.ItemName?.Trim() ?? string.Empty,
                    ItemType = string.IsNullOrWhiteSpace(dto.ItemType) ? "منتج تام" : dto.ItemType,
                    Unit = string.IsNullOrWhiteSpace(dto.Unit) ? "قطعة" : dto.Unit,
                    Active = dto.Active,
                    HasExpiryDate = dto.HasExpiryDate,
                    ShelfLifeDays = dto.HasExpiryDate ? (dto.ShelfLifeDays ?? 4) : null,
                    ReorderLevel = dto.ReorderLevel,
                    SafetyStock = dto.SafetyStock,
                    MaxStock = dto.MaxStock,
                    BasePrice = dto.BasePrice,
                    Barcode = string.IsNullOrWhiteSpace(dto.Barcode) ? null : dto.Barcode.Trim(),
                    IsVariant = dto.HasVariants,
                    IsMerged = dto.IsMerged,
                    IsContainer = dto.IsContainer,
                    MultiUom = dto.MultiUom,
                    HideDetailPrint = dto.HideDetailPrint,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Items.Add(item);
                await _context.SaveChangesAsync();

                // إضافة النكهات
                if (dto.HasVariants && dto.Variants?.Any() == true)
                {
                    int vIndex = 1;
                    foreach (var v in dto.Variants)
                    {
                        var child = new Item
                        {
                            CompanyId = companyId,
                            CategoryId = categoryId,
                            DepartmentId = targetDeptId,
                            DepartmentName = targetDeptName,
                            ParentItemId = item.Id,
                            ItemCode = !string.IsNullOrWhiteSpace(v.VariantCode)
                                ? v.VariantCode.Trim()
                                : $"{item.ItemCode}-{vIndex:D2}",
                            ItemName = !string.IsNullOrWhiteSpace(v.VariantName)
                                ? v.VariantName.Trim()
                                : $"{item.ItemName} نوع {vIndex}",
                            ItemType = item.ItemType,
                            Unit = item.Unit,
                            BasePrice = (v.PriceOverride.HasValue && v.PriceOverride.Value > 0) ? v.PriceOverride.Value : item.BasePrice,
                            Barcode = !string.IsNullOrWhiteSpace(v.Barcode) ? v.Barcode.Trim() : item.Barcode,
                            IsVariant = true,
                            Active = item.Active,
                            CreatedAt = DateTime.UtcNow
                        };
                        _context.Items.Add(child);
                        vIndex++;
                    }
                    await _context.SaveChangesAsync();
                }

                await transaction.CommitAsync();
                return CreatedAtAction(nameof(GetItem), new { id = item.Id }, new { success = true, id = item.Id });
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        // ==============================================================
        // 4. تعديل صنف
        // ==============================================================
        [HttpPut("{id:long}")]
        public async Task<IActionResult> PutItem(long id, [FromBody] ItemApiDto dto)
        {
            var item = await _context.Items
                .Include(i => i.Variants)
                .FirstOrDefaultAsync(i => i.Id == id);

            if (item == null)
                return NotFound(new { message = "الصنف غير موجود." });

            // تأكيد ملكية الصنف للشركة
            if (item.CompanyId != _tenantContext.CompanyId)
                return Forbid();

            // فحص التكرار داخل نفس الشركة
            if (!string.IsNullOrWhiteSpace(dto.ItemCode))
            {
                var codeExists = await _context.Items
                    .AnyAsync(i => i.Id != id &&
                                   i.CompanyId == item.CompanyId &&
                                   i.ItemCode.ToLower() == dto.ItemCode.Trim().ToLower());
                if (codeExists)
                    return Conflict(new { message = $"كود الصنف [{dto.ItemCode.Trim()}] مسجل مسبقاً." });
            }

            // تأكيد DepartmentId
            if (dto.DepartmentId.HasValue && dto.DepartmentId.Value > 0)
            {
                var dept = await _context.Departments
                    .FirstOrDefaultAsync(d => d.Id == dto.DepartmentId.Value && d.CompanyId == item.CompanyId);
                if (dept != null)
                {
                    item.DepartmentId = dept.Id;
                    item.DepartmentName = dept.DepartmentName;
                }
            }

            // تأكيد CategoryId
            if (dto.CategoryId > 0)
            {
                var catExists = await _context.ItemCategories
                    .AnyAsync(c => c.Id == dto.CategoryId && c.CompanyId == item.CompanyId);
                if (catExists) item.CategoryId = dto.CategoryId;
            }

            // نحدّث الحقول المسموح بها
            item.ItemCode = dto.ItemCode?.Trim() ?? item.ItemCode;
            item.ItemName = dto.ItemName?.Trim() ?? item.ItemName;
            if (!string.IsNullOrWhiteSpace(dto.ItemType)) item.ItemType = dto.ItemType;
            if (!string.IsNullOrWhiteSpace(dto.Unit)) item.Unit = dto.Unit;
            item.Active = dto.Active;
            item.BasePrice = dto.BasePrice;
            item.SafetyStock = dto.SafetyStock;
            item.ReorderLevel = dto.ReorderLevel;
            item.Barcode = string.IsNullOrWhiteSpace(dto.Barcode) ? null : dto.Barcode.Trim();
            item.IsVariant = dto.HasVariants;

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // مزامنة النكهات
                var existingVariants = await _context.Items
                    .Where(v => v.ParentItemId == item.Id)
                    .ToListAsync();

                if (dto.HasVariants && dto.Variants?.Any() == true)
                {
                    var processedCodes = new HashSet<string>();

                    foreach (var v in dto.Variants)
                    {
                        var code = !string.IsNullOrWhiteSpace(v.VariantCode) ? v.VariantCode.Trim() : "";
                        var existing = existingVariants.FirstOrDefault(x =>
                            (!string.IsNullOrEmpty(code) && x.ItemCode == code) ||
                            (v.Id > 0 && x.Id == v.Id));

                        if (existing != null)
                        {
                            existing.ItemName = v.VariantName.Trim();
                            existing.BasePrice = (v.PriceOverride.HasValue && v.PriceOverride.Value > 0)
                                ? v.PriceOverride.Value
                                : item.BasePrice;
                            existing.Barcode = !string.IsNullOrWhiteSpace(v.Barcode) ? v.Barcode.Trim() : item.Barcode;
                            existing.Active = item.Active;
                            existing.DepartmentId = item.DepartmentId;
                            existing.DepartmentName = item.DepartmentName;
                            processedCodes.Add(existing.ItemCode);
                        }
                        else
                        {
                            var newChild = new Item
                            {
                                CompanyId = item.CompanyId,
                                CategoryId = item.CategoryId,
                                DepartmentId = item.DepartmentId,
                                DepartmentName = item.DepartmentName,
                                ParentItemId = item.Id,
                                ItemCode = !string.IsNullOrWhiteSpace(code)
                                    ? code
                                    : $"{item.ItemCode}-{(existingVariants.Count + processedCodes.Count + 1):D2}",
                                ItemName = !string.IsNullOrWhiteSpace(v.VariantName)
                                    ? v.VariantName.Trim()
                                    : $"{item.ItemName} نوع",
                                ItemType = item.ItemType,
                                Unit = item.Unit,
                                BasePrice = (v.PriceOverride.HasValue && v.PriceOverride.Value > 0) ? v.PriceOverride.Value : item.BasePrice,
                                Barcode = !string.IsNullOrWhiteSpace(v.Barcode) ? v.Barcode.Trim() : item.Barcode,
                                IsVariant = true,
                                Active = item.Active,
                                CreatedAt = DateTime.UtcNow
                            };
                            _context.Items.Add(newChild);
                            processedCodes.Add(newChild.ItemCode);
                        }
                    }

                    var toRemove = existingVariants.Where(x => !processedCodes.Contains(x.ItemCode)).ToList();
                    if (toRemove.Any())
                        _context.Items.RemoveRange(toRemove);
                }
                else
                {
                    if (existingVariants.Any())
                        _context.Items.RemoveRange(existingVariants);
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Ok(new { success = true });
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        // ==============================================================
        // 5. الاستيراد السريع (Bulk Import)
        // ==============================================================
        [HttpPost("bulk-quick-import")]
        public async Task<IActionResult> BulkQuickImport([FromBody] BulkQuickImportRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.RawData))
                return BadRequest(new { message = "البيانات النصية فارغة." });

            var lines = request.RawData.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);
            if (!lines.Any())
                return BadRequest(new { message = "لا توجد أسطر صالحة للاستيراد." });

            var companyId = _tenantContext.CompanyId;

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var departments = await _context.Departments
                    .Where(d => d.CompanyId == companyId)
                    .ToListAsync();

                int processedCount = 0;

                foreach (var line in lines)
                {
                    var text = line.Trim();
                    if (string.IsNullOrWhiteSpace(text) ||
                        text.StartsWith("اسم الصنف") ||
                        text.StartsWith("اسم_الصنف"))
                        continue;

                    var cols = text.Split('|').Select(c => c.Trim()).ToList();
                    if (cols.Count < 2) cols = text.Split('\t').Select(c => c.Trim()).ToList();
                    if (cols.Count < 2) cols = text.Split(',').Select(c => c.Trim()).ToList();
                    if (cols.Count < 2) continue;

                    string itemName = cols[0];
                    string itemCode = cols.Count > 1 ? cols[1] : string.Empty;
                    string deptName = cols.Count > 2 ? cols[2] : string.Empty;
                    string variantName = cols.Count > 3 ? cols[3] : string.Empty;
                    string unit = cols.Count > 4 && !string.IsNullOrEmpty(cols[4]) ? cols[4] : "قطعة";
                    decimal.TryParse(cols.Count > 5 ? cols[5] : "1", out decimal packingFactor);
                    decimal.TryParse(cols.Count > 6 ? cols[6] : "0", out decimal price);
                    string barcode = cols.Count > 7 ? cols[7] : string.Empty;

                    if (string.IsNullOrWhiteSpace(itemCode))
                        itemCode = $"FG-{Guid.NewGuid().ToString("N")[..6].ToUpper()}";

                    // مطابقة القسم (داخل نفس الشركة)
                    var dept = departments.FirstOrDefault(d =>
                        d.DepartmentName.Trim().Equals(deptName, StringComparison.OrdinalIgnoreCase));

                    long? deptId = dept?.Id;

                    // جلب أو إنشاء الصنف الأساسي
                    var item = await _context.Items.FirstOrDefaultAsync(i =>
                        i.CompanyId == companyId &&
                        i.ItemCode.ToLower() == itemCode.ToLower() &&
                        i.ParentItemId == null);

                    if (item == null)
                    {
                        item = new Item
                        {
                            CompanyId = companyId,
                            CategoryId = request.DefaultCategoryId,
                            DepartmentId = deptId,
                            DepartmentName = !string.IsNullOrEmpty(deptName) ? deptName : (dept?.DepartmentName ?? "صالة الإنتاج"),
                            ItemCode = itemCode,
                            ItemName = itemName,
                            ItemType = "منتج تام",
                            Unit = unit,
                            BasePrice = price,
                            Barcode = !string.IsNullOrEmpty(barcode) ? barcode : null,
                            Active = true,
                            HasExpiryDate = true,
                            ShelfLifeDays = 4,
                            IsVariant = !string.IsNullOrWhiteSpace(variantName),
                            CreatedAt = DateTime.UtcNow
                        };
                        _context.Items.Add(item);
                        await _context.SaveChangesAsync();
                    }
                    else
                    {
                        item.ItemName = itemName;
                        item.BasePrice = price > 0 ? price : item.BasePrice;
                        item.Unit = unit;
                        if (deptId.HasValue) item.DepartmentId = deptId;
                        if (!string.IsNullOrEmpty(barcode)) item.Barcode = barcode;
                        if (!string.IsNullOrWhiteSpace(variantName)) item.IsVariant = true;
                    }

                    // النكهة
                    if (!string.IsNullOrWhiteSpace(variantName))
                    {
                        var existsVar = await _context.Items.AnyAsync(v =>
                            v.ParentItemId == item.Id &&
                            v.ItemName.ToLower() == variantName.ToLower());

                        if (!existsVar)
                        {
                            var vItem = new Item
                            {
                                CompanyId = companyId,
                                CategoryId = item.CategoryId,
                                DepartmentId = item.DepartmentId,
                                DepartmentName = item.DepartmentName,
                                ParentItemId = item.Id,
                                ItemCode = $"{item.ItemCode}-V{Guid.NewGuid().ToString("N")[..4].ToUpper()}",
                                ItemName = variantName,
                                ItemType = "منتج تام",
                                Unit = unit,
                                BasePrice = price > 0 ? price : item.BasePrice,
                                Barcode = !string.IsNullOrEmpty(barcode) ? barcode : null,
                                IsVariant = true,
                                Active = true,
                                CreatedAt = DateTime.UtcNow
                            };
                            _context.Items.Add(vItem);
                        }
                    }

                    // معامل التعبئة
                    if (packingFactor > 1)
                    {
                        var hasConv = await _context.ItemUomConversions
                            .AnyAsync(c => c.ItemId == item.Id && c.FromUnit == "بلكة");

                        if (!hasConv)
                        {
                            _context.ItemUomConversions.Add(new ItemUomConversion
                            {
                                CompanyId = companyId,
                                ItemId = item.Id,
                                FromUnit = "بلكة",
                                ToUnit = unit,
                                ConversionFactor = packingFactor,
                                Notes = $"معامل تعبئة قياسي للصنف {item.ItemName}",
                                CreatedAt = DateTime.UtcNow
                            });
                        }
                    }

                    processedCount++;
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Ok(new
                {
                    success = true,
                    message = $"✅ تم استيراد ومعالجة ({processedCount}) صنفاً بنجاح."
                });
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        // ==============================================================
        // 6. حذف الصنف (Soft Delete)
        // ==============================================================
        [HttpDelete("{id:long}")]
        public async Task<IActionResult> DeleteItem(long id)
        {
            var item = await _context.Items
                .Include(i => i.Variants)
                .FirstOrDefaultAsync(i => i.Id == id);

            if (item == null)
                return NotFound(new { message = "الصنف غير موجود." });

            // تأكيد ملكية الصنف للشركة
            if (item.CompanyId != _tenantContext.CompanyId)
                return Forbid();

            // فحص الحركات المخزنية
            bool hasMovements = await _context.StockMovements.AnyAsync(m => m.ItemId == id);
            if (hasMovements)
                return BadRequest(new { message = "لا يمكن حذف الصنف لوجود حركات مخزنية مرتبطة به. يمكنك تعطيله بدلاً من الحذف." });

            // Soft Delete للصنف والنكهات
            item.Active = false;
            foreach (var variant in item.Variants)
                variant.Active = false;

            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "تم تعطيل الصنف بنجاح." });
        }
    }
}