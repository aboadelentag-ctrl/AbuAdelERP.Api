using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using KayanERP.Api.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KayanERP.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ItemsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ItemsController(AppDbContext context)
        {
            _context = context;
        }

        // ==============================================================
        // نماذج البيانات (DTOs)
        // ==============================================================
        public class ItemVariantDto
        {
            public long Id { get; set; }
            public string VariantCode { get; set; } = string.Empty;
            public string VariantName { get; set; } = string.Empty;
            public decimal? PriceOverride { get; set; }
            public string? Barcode { get; set; }
        }

        public class ItemUnitConversionDto
        {
            public string UnitName { get; set; } = string.Empty;
            public decimal ConversionFactor { get; set; } = 1;
            public string? Barcode { get; set; }
        }

        public class ItemApiDto
        {
            public long Id { get; set; }
            public long CompanyId { get; set; } = 1;
            public long CategoryId { get; set; } = 1;
            public long? DepartmentId { get; set; }
            public string? DepartmentName { get; set; }
            public string ItemCode { get; set; } = string.Empty;
            public string ItemName { get; set; } = string.Empty;
            public string ItemType { get; set; } = "منتج تام";
            public string Unit { get; set; } = "كجم";
            public bool Active { get; set; } = true;
            public bool HasExpiryDate { get; set; } = true;
            public int? ShelfLifeDays { get; set; }

            public decimal ReorderLevel { get; set; } = 0;
            public decimal ReorderPoint { get; set; } = 0;
            public decimal SafetyStock { get; set; } = 0;
            public decimal SafetyStockLevel { get; set; } = 0;
            public decimal MaxStock { get; set; } = 0;

            public decimal BasePrice { get; set; } = 0;
            public decimal SalePrice { get; set; } = 0;
            public decimal PurchasePrice { get; set; } = 0;
            public string? Barcode { get; set; }
            public string? Description { get; set; }

            public bool HasVariants { get; set; } = false;
            public bool IsVariant { get; set; } = false;
            public bool IsMerged { get; set; } = false;
            public bool IsContainer { get; set; } = false;
            public bool MultiUom { get; set; } = false;
            public bool HideDetailPrint { get; set; } = false;
            public long? ParentItemId { get; set; }

            public List<ItemVariantDto> Variants { get; set; } = new();
            public List<ItemUnitConversionDto> UnitsHierarchy { get; set; } = new();
        }

        public class BulkQuickImportRequest
        {
            public string RawData { get; set; } = string.Empty;
            public long CompanyId { get; set; } = 1;
            public long DefaultCategoryId { get; set; } = 1;
        }

        // ==============================================================
        // 1. جلب قائمة الأصناف مع النكهات والأنواع التابعة لها
        // ==============================================================
        [HttpGet]
        public async Task<ActionResult<List<ItemApiDto>>> GetItems([FromQuery] string? itemType = null)
        {
            try
            {
                var query = _context.Items
                    .AsNoTracking()
                    .Where(i => i.ParentItemId == null)
                    .Include(i => i.Department)
                    .Include(i => i.Variants)
                    .AsQueryable();

                if (!string.IsNullOrWhiteSpace(itemType))
                {
                    query = query.Where(i => i.ItemType == itemType);
                }

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
                        ReorderPoint = i.ReorderLevel,
                        SafetyStock = i.SafetyStock,
                        SafetyStockLevel = i.SafetyStock,
                        MaxStock = i.MaxStock,
                        BasePrice = i.BasePrice,
                        SalePrice = i.BasePrice,
                        Barcode = i.Barcode,
                        Description = string.Empty,
                        HasVariants = i.IsVariant || i.Variants.Any(),
                        IsVariant = i.IsVariant,
                        IsMerged = i.IsMerged,
                        IsContainer = i.IsContainer,
                        MultiUom = i.MultiUom,
                        HideDetailPrint = i.HideDetailPrint,
                        ParentItemId = i.ParentItemId,
                        Variants = i.Variants.Select(v => new ItemVariantDto
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
            catch (Exception ex)
            {
                return StatusCode(500, $"خطأ في جلب الأصناف: {ex.GetBaseException().Message}");
            }
        }

        // ==============================================================
        // 2. جلب صنف محدد مع نكهاته
        // ==============================================================
        [HttpGet("{id}")]
        public async Task<ActionResult<ItemApiDto>> GetItem(long id)
        {
            try
            {
                var item = await _context.Items
                    .Include(i => i.Department)
                    .Include(i => i.Variants)
                    .FirstOrDefaultAsync(i => i.Id == id);

                if (item == null) return NotFound("الصنف غير موجود.");

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
                    ReorderPoint = item.ReorderLevel,
                    SafetyStock = item.SafetyStock,
                    SafetyStockLevel = item.SafetyStock,
                    MaxStock = item.MaxStock,
                    BasePrice = item.BasePrice,
                    SalePrice = item.BasePrice,
                    Barcode = item.Barcode,
                    Description = string.Empty,
                    HasVariants = item.IsVariant || item.Variants.Any(),
                    IsVariant = item.IsVariant,
                    IsMerged = item.IsMerged,
                    IsContainer = item.IsContainer,
                    MultiUom = item.MultiUom,
                    HideDetailPrint = item.HideDetailPrint,
                    ParentItemId = item.ParentItemId,
                    Variants = item.Variants.Select(v => new ItemVariantDto
                    {
                        Id = v.Id,
                        VariantCode = v.ItemCode,
                        VariantName = v.ItemName,
                        PriceOverride = v.BasePrice,
                        Barcode = v.Barcode
                    }).ToList()
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"خطأ في جلب الصنف: {ex.GetBaseException().Message}");
            }
        }

        // ==============================================================
        // 3. تعديل صنف قائم ومزامنة نكهاته وتصنيفه بدقة
        // ==============================================================
        [HttpPut("{id}")]
        public async Task<IActionResult> PutItem(long id, [FromBody] ItemApiDto dto)
        {
            if (dto == null) return BadRequest("بيانات غير صالحة.");

            if (!string.IsNullOrWhiteSpace(dto.ItemCode))
            {
                var codeExists = await _context.Items.AnyAsync(i => i.Id != id && i.ItemCode.ToLower() == dto.ItemCode.Trim().ToLower());
                if (codeExists)
                    return BadRequest($"كود الصنف [{dto.ItemCode.Trim()}] مسجل مسبقاً لصنف آخر. يرجى اختيار كود فريد.");
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var item = await _context.Items
                    .Include(i => i.Variants)
                    .FirstOrDefaultAsync(i => i.Id == id);

                if (item == null) return NotFound("الصنف غير موجود.");

                if (dto.DepartmentId.HasValue && dto.DepartmentId.Value > 0)
                {
                    item.DepartmentId = dto.DepartmentId;
                    var dept = await _context.Departments.FindAsync(dto.DepartmentId.Value);
                    item.DepartmentName = dept?.DepartmentName ?? dto.DepartmentName;
                }
                else if (!string.IsNullOrWhiteSpace(dto.DepartmentName))
                {
                    var dept = await _context.Departments.FirstOrDefaultAsync(d => d.DepartmentName.Trim() == dto.DepartmentName.Trim());
                    item.DepartmentId = dept?.Id;
                    item.DepartmentName = dept?.DepartmentName ?? dto.DepartmentName;
                }

                if (dto.CategoryId > 0)
                {
                    item.CategoryId = dto.CategoryId;
                }

                item.ItemCode = dto.ItemCode?.Trim() ?? string.Empty;
                item.ItemName = dto.ItemName?.Trim() ?? string.Empty;
                item.ItemType = string.IsNullOrWhiteSpace(dto.ItemType) ? item.ItemType : dto.ItemType;
                item.Unit = string.IsNullOrWhiteSpace(dto.Unit) ? item.Unit : dto.Unit;
                item.Active = dto.Active;
                item.BasePrice = dto.SalePrice > 0 ? dto.SalePrice : dto.BasePrice;
                item.SafetyStock = dto.SafetyStockLevel > 0 ? dto.SafetyStockLevel : dto.SafetyStock;
                item.ReorderLevel = dto.ReorderPoint > 0 ? dto.ReorderPoint : dto.ReorderLevel;
                item.Barcode = string.IsNullOrWhiteSpace(dto.Barcode) ? null : dto.Barcode.Trim();
                item.IsVariant = dto.HasVariants;

                var existingVariants = await _context.Items.Where(v => v.ParentItemId == item.Id).ToListAsync();

                if (dto.HasVariants && dto.Variants != null && dto.Variants.Any())
                {
                    var processedCodes = new HashSet<string>();

                    foreach (var v in dto.Variants)
                    {
                        var code = !string.IsNullOrWhiteSpace(v.VariantCode) ? v.VariantCode.Trim() : "";
                        var existing = existingVariants.FirstOrDefault(x => (!string.IsNullOrEmpty(code) && x.ItemCode == code) || (v.Id > 0 && x.Id == v.Id));

                        if (existing != null)
                        {
                            existing.ItemName = v.VariantName.Trim();
                            existing.BasePrice = (v.PriceOverride.HasValue && v.PriceOverride.Value > 0) ? v.PriceOverride.Value : item.BasePrice;
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
                                ItemCode = !string.IsNullOrWhiteSpace(code) ? code : $"{item.ItemCode}-{(existingVariants.Count + processedCodes.Count + 1):D2}",
                                ItemName = !string.IsNullOrWhiteSpace(v.VariantName) ? v.VariantName.Trim() : $"{item.ItemName} نوع",
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
                    {
                        _context.Items.RemoveRange(toRemove);
                    }
                }
                else
                {
                    if (existingVariants.Any())
                    {
                        _context.Items.RemoveRange(existingVariants);
                    }
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                return Ok(true);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, $"خطأ أثناء تحديث الصنف ونكهاته: {ex.GetBaseException().Message}");
            }
        }

        // ==============================================================
        // 4. إضافة صنف جديد
        // ==============================================================
        [HttpPost]
        public async Task<ActionResult> PostItem([FromBody] ItemApiDto dto)
        {
            if (dto == null) return BadRequest("بيانات غير صالحة.");

            if (string.IsNullOrWhiteSpace(dto.ItemCode))
                return BadRequest("كود الصنف مطلوب.");

            var exists = await _context.Items.AnyAsync(i => i.ItemCode.ToLower() == dto.ItemCode.Trim().ToLower());
            if (exists)
                return BadRequest($"كود الصنف [{dto.ItemCode.Trim()}] مسجل مسبقاً لصنف آخر. يرجى اختيار كود فريد.");

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var companyId = dto.CompanyId > 0 ? dto.CompanyId : ((await _context.Companies.FirstOrDefaultAsync())?.Id ?? 1);
                var categoryId = dto.CategoryId > 0 ? dto.CategoryId : ((await _context.ItemCategories.FirstOrDefaultAsync())?.Id ?? 1);

                long? targetDeptId = (dto.DepartmentId.HasValue && dto.DepartmentId.Value > 0) ? dto.DepartmentId : null;
                string? targetDeptName = dto.DepartmentName;

                if (targetDeptId.HasValue)
                {
                    var d = await _context.Departments.FindAsync(targetDeptId.Value);
                    if (d != null) targetDeptName = d.DepartmentName;
                }
                else if (!string.IsNullOrWhiteSpace(targetDeptName))
                {
                    var d = await _context.Departments.FirstOrDefaultAsync(x => x.DepartmentName.Trim() == targetDeptName.Trim());
                    if (d != null)
                    {
                        targetDeptId = d.Id;
                        targetDeptName = d.DepartmentName;
                    }
                }

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
                    ReorderLevel = dto.ReorderPoint > 0 ? dto.ReorderPoint : dto.ReorderLevel,
                    SafetyStock = dto.SafetyStockLevel > 0 ? dto.SafetyStockLevel : dto.SafetyStock,
                    MaxStock = dto.MaxStock,
                    BasePrice = dto.SalePrice > 0 ? dto.SalePrice : dto.BasePrice,
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

                if (dto.HasVariants && dto.Variants != null && dto.Variants.Any())
                {
                    int vIndex = 1;
                    foreach (var v in dto.Variants)
                    {
                        var child = new Item
                        {
                            CompanyId = item.CompanyId,
                            CategoryId = item.CategoryId,
                            DepartmentId = item.DepartmentId,
                            DepartmentName = item.DepartmentName,
                            ParentItemId = item.Id,
                            ItemCode = !string.IsNullOrWhiteSpace(v.VariantCode) ? v.VariantCode.Trim() : $"{item.ItemCode}-{vIndex:D2}",
                            ItemName = !string.IsNullOrWhiteSpace(v.VariantName) ? v.VariantName.Trim() : $"{item.ItemName} نوع {vIndex}",
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
                return Ok(new { success = true, id = item.Id });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, $"خطأ قاعدة البيانات: {ex.GetBaseException().Message}");
            }
        }

        // ==============================================================
        // 5. الاستيراد السريع لدفعات الأصناف من نص خام (Bulk Quick Import)
        // ==============================================================
        [HttpPost("bulk-quick-import")]
        public async Task<IActionResult> BulkQuickImport([FromBody] BulkQuickImportRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.RawData))
                return BadRequest("البيانات النصية المرسلة فارغة.");

            var lines = request.RawData.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);
            if (!lines.Any())
                return BadRequest("لا توجد أسطر بيانات صالحة للاستيراد.");

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var departments = await _context.Departments.ToListAsync();
                int processedCount = 0;

                foreach (var line in lines)
                {
                    var text = line.Trim();
                    if (string.IsNullOrWhiteSpace(text) || text.StartsWith("اسم الصنف") || text.StartsWith("اسم_الصنف"))
                        continue;

                    var cols = text.Split('|').Select(c => c.Trim()).ToList();
                    if (cols.Count < 2)
                        cols = text.Split('\t').Select(c => c.Trim()).ToList();
                    if (cols.Count < 2)
                        cols = text.Split(',').Select(c => c.Trim()).ToList();

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
                        itemCode = $"FG-{new Random().Next(10000, 99999)}";

                    // مطابقة القسم
                    var dept = departments.FirstOrDefault(d => d.DepartmentName.Trim().Equals(deptName, StringComparison.OrdinalIgnoreCase));
                    long? deptId = dept?.Id;

                    // جلب أو إنشاء الصنف الأساسي
                    var item = await _context.Items.FirstOrDefaultAsync(i => i.ItemCode.ToLower() == itemCode.ToLower() && i.ParentItemId == null);
                    if (item == null)
                    {
                        item = new Item
                        {
                            CompanyId = request.CompanyId,
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

                    // إضافة التفرع إن وُجد
                    if (!string.IsNullOrWhiteSpace(variantName))
                    {
                        var variantCode = $"{item.ItemCode}-V{new Random().Next(10, 99)}";
                        var existsVar = await _context.Items.AnyAsync(v => v.ParentItemId == item.Id && v.ItemName.ToLower() == variantName.ToLower());
                        if (!existsVar)
                        {
                            var vItem = new Item
                            {
                                CompanyId = item.CompanyId,
                                CategoryId = item.CategoryId,
                                DepartmentId = item.DepartmentId,
                                DepartmentName = item.DepartmentName,
                                ParentItemId = item.Id,
                                ItemCode = variantCode,
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

                    // إضافة معامل التعبئة إن وُجد
                    if (packingFactor > 1)
                    {
                        var hasConv = await _context.ItemUomConversions.AnyAsync(c => c.ItemId == item.Id && c.FromUnit == "بلكة");
                        if (!hasConv)
                        {
                            _context.ItemUomConversions.Add(new ItemUomConversion
                            {
                                CompanyId = item.CompanyId,
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

                return Ok(new { success = true, message = $"✅ تم استيراد ومعالجة ({processedCount}) صنفاً بنجاح مع النكهات ومعاملات التعبئة." });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, $"فشل الاستيراد السريع: {ex.GetBaseException().Message}");
            }
        }

        // ==============================================================
        // 6. حذف الصنف
        // ==============================================================
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteItem(long id)
        {
            try
            {
                var item = await _context.Items
                    .Include(i => i.Variants)
                    .FirstOrDefaultAsync(i => i.Id == id);

                if (item == null) return NotFound("الصنف غير موجود.");

                bool hasMovements = await _context.StockMovements.AnyAsync(m => m.ItemId == id);
                if (hasMovements)
                {
                    return BadRequest("لا يمكن حذف الصنف لوجود حركات مخزنية مرتبطة به في النظام. يمكنك إيقافه أو تعطيله بدلاً من الحذف.");
                }

                if (item.Variants.Any())
                {
                    _context.Items.RemoveRange(item.Variants);
                }

                _context.Items.Remove(item);
                await _context.SaveChangesAsync();

                return Ok(new { success = true, message = "تم حذف الصنف بنجاح." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"خطأ أثناء حذف الصنف: {ex.GetBaseException().Message}");
            }
        }
    }
}