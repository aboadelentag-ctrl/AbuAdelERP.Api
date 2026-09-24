using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AbuAdelERP.Api.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace AbuAdelERP.Api.Controllers
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
        public class ItemApiDto
        {
            public long Id { get; set; }
            public long CompanyId { get; set; }
            public long CategoryId { get; set; }
            public long? DepartmentId { get; set; }
            public string? DepartmentName { get; set; }
            public string ItemCode { get; set; } = string.Empty;
            public string ItemName { get; set; } = string.Empty;
            public string ItemType { get; set; } = string.Empty;
            public string Unit { get; set; } = string.Empty;
            public bool Active { get; set; } = true;
            public bool HasExpiryDate { get; set; } = true;
            public int? ShelfLifeDays { get; set; }

            // محددات حدود المخزون والطلب
            public decimal ReorderLevel { get; set; } = 0;
            public decimal SafetyStock { get; set; } = 0;
            public decimal MaxStock { get; set; } = 0;

            // الخصائص التوافقية مع نظام كيان وقاعدة البيانات
            public decimal BasePrice { get; set; } = 0;
            public string? Barcode { get; set; }
            public bool IsVariant { get; set; } = false;
            public bool IsMerged { get; set; } = false;
            public bool IsContainer { get; set; } = false;
            public bool MultiUom { get; set; } = false;
            public bool HideDetailPrint { get; set; } = false;
            public long? ParentItemId { get; set; }
        }

        public class BulkImportRequestDto
        {
            public string RawData { get; set; } = string.Empty;
            public long CompanyId { get; set; } = 1;
            public long DefaultCategoryId { get; set; } = 1;
        }

        // ==============================================================
        // 1. جلب قائمة الأصناف مع الفلترة الاختيارية
        // ==============================================================
        [HttpGet]
        public async Task<ActionResult<List<ItemApiDto>>> GetItems([FromQuery] string? itemType = null)
        {
            try
            {
                var unconfiguredFoodItems = await _context.Items
                    .Where(i => i.ItemType != "مواد تعبئة" && !i.HasExpiryDate && i.ShelfLifeDays == null)
                    .ToListAsync();

                if (unconfiguredFoodItems.Any())
                {
                    foreach (var item in unconfiguredFoodItems)
                    {
                        item.HasExpiryDate = true;
                        item.ShelfLifeDays = (item.ItemType == "خامات") ? 365 : 4;
                    }
                    await _context.SaveChangesAsync();
                }

                var query = _context.Items.AsNoTracking().AsQueryable();

                if (!string.IsNullOrWhiteSpace(itemType))
                {
                    query = query.Where(i => i.ItemType == itemType);
                }

                var items = await query
                    .Include(i => i.Department)
                    .OrderBy(i => i.Id)
                    .Select(i => new ItemApiDto
                    {
                        Id = i.Id,
                        CompanyId = i.CompanyId,
                        CategoryId = i.CategoryId,
                        DepartmentId = i.DepartmentId,
                        DepartmentName = i.Department != null ? i.Department.DepartmentName : i.DepartmentName,
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
                        IsVariant = i.IsVariant,
                        IsMerged = i.IsMerged,
                        IsContainer = i.IsContainer,
                        MultiUom = i.MultiUom,
                        HideDetailPrint = i.HideDetailPrint,
                        ParentItemId = i.ParentItemId
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
        // 2. جلب صنف محدد
        // ==============================================================
        [HttpGet("{id}")]
        public async Task<ActionResult<ItemApiDto>> GetItem(long id)
        {
            try
            {
                var item = await _context.Items
                    .Include(i => i.Department)
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
                    SafetyStock = item.SafetyStock,
                    MaxStock = item.MaxStock,
                    BasePrice = item.BasePrice,
                    Barcode = item.Barcode,
                    IsVariant = item.IsVariant,
                    IsMerged = item.IsMerged,
                    IsContainer = item.IsContainer,
                    MultiUom = item.MultiUom,
                    HideDetailPrint = item.HideDetailPrint,
                    ParentItemId = item.ParentItemId
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"خطأ في جلب الصنف: {ex.GetBaseException().Message}");
            }
        }

        // ==============================================================
        // 3. تعديل صنف قائم
        // ==============================================================
        [HttpPut("{id}")]
        public async Task<IActionResult> PutItem(long id, [FromBody] ItemApiDto dto)
        {
            if (dto == null) return BadRequest("بيانات غير صالحة.");

            try
            {
                var item = await _context.Items.FindAsync(id);
                if (item == null) return NotFound("الصنف غير موجود.");

                item.CompanyId = dto.CompanyId > 0 ? dto.CompanyId : item.CompanyId;
                item.CategoryId = dto.CategoryId > 0 ? dto.CategoryId : item.CategoryId;
                item.DepartmentId = (dto.DepartmentId.HasValue && dto.DepartmentId.Value > 0) ? dto.DepartmentId : null;
                item.DepartmentName = dto.DepartmentName;
                item.ItemCode = dto.ItemCode?.Trim() ?? string.Empty;
                item.ItemName = dto.ItemName?.Trim() ?? string.Empty;
                item.ItemType = string.IsNullOrWhiteSpace(dto.ItemType) ? item.ItemType : dto.ItemType;
                item.Unit = string.IsNullOrWhiteSpace(dto.Unit) ? item.Unit : dto.Unit;
                item.Active = dto.Active;

                item.HasExpiryDate = dto.HasExpiryDate;
                item.ShelfLifeDays = dto.HasExpiryDate ? dto.ShelfLifeDays : null;

                item.ReorderLevel = dto.ReorderLevel;
                item.SafetyStock = dto.SafetyStock;
                item.MaxStock = dto.MaxStock;

                item.BasePrice = dto.BasePrice;
                item.Barcode = string.IsNullOrWhiteSpace(dto.Barcode) ? null : dto.Barcode.Trim();
                item.IsVariant = dto.IsVariant;
                item.IsMerged = dto.IsMerged;
                item.IsContainer = dto.IsContainer;
                item.MultiUom = dto.MultiUom;
                item.HideDetailPrint = dto.HideDetailPrint;
                item.ParentItemId = (dto.ParentItemId.HasValue && dto.ParentItemId.Value > 0) ? dto.ParentItemId : null;

                await _context.SaveChangesAsync();
                return Ok(true);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"خطأ أثناء تحديث الصنف: {ex.GetBaseException().Message}");
            }
        }

        // ==============================================================
        // 4. إضافة صنف جديد (معالجة حقول الـ Foreign Key والـ Unique)
        // ==============================================================
        [HttpPost]
        public async Task<ActionResult> PostItem([FromBody] ItemApiDto dto)
        {
            if (dto == null) return BadRequest("بيانات غير صالحة.");

            try
            {
                // ضمان وجود CompanyId و CategoryId صالحين من قاعدة البيانات
                var companyId = dto.CompanyId > 0 ? dto.CompanyId : ((await _context.Companies.FirstOrDefaultAsync())?.Id ?? 1);
                var categoryId = dto.CategoryId > 0 ? dto.CategoryId : ((await _context.ItemCategories.FirstOrDefaultAsync())?.Id ?? 1);

                var item = new Item
                {
                    CompanyId = companyId,
                    CategoryId = categoryId,
                    DepartmentId = (dto.DepartmentId.HasValue && dto.DepartmentId.Value > 0) ? dto.DepartmentId : null,
                    DepartmentName = dto.DepartmentName,
                    ItemCode = dto.ItemCode?.Trim() ?? string.Empty,
                    ItemName = dto.ItemName?.Trim() ?? string.Empty,
                    ItemType = string.IsNullOrWhiteSpace(dto.ItemType) ? "منتج تام" : dto.ItemType,
                    Unit = string.IsNullOrWhiteSpace(dto.Unit) ? "قطعة" : dto.Unit,
                    Active = dto.Active,
                    HasExpiryDate = dto.HasExpiryDate,
                    ShelfLifeDays = dto.HasExpiryDate ? dto.ShelfLifeDays : null,
                    ReorderLevel = dto.ReorderLevel,
                    SafetyStock = dto.SafetyStock,
                    MaxStock = dto.MaxStock,
                    BasePrice = dto.BasePrice,
                    Barcode = string.IsNullOrWhiteSpace(dto.Barcode) ? null : dto.Barcode.Trim(),
                    IsVariant = dto.IsVariant,
                    IsMerged = dto.IsMerged,
                    IsContainer = dto.IsContainer,
                    MultiUom = dto.MultiUom,
                    HideDetailPrint = dto.HideDetailPrint,
                    ParentItemId = (dto.ParentItemId.HasValue && dto.ParentItemId.Value > 0) ? dto.ParentItemId : null,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Items.Add(item);
                await _context.SaveChangesAsync();
                return Ok(new { success = true, id = item.Id });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"خطأ قاعدة البيانات: {ex.GetBaseException().Message}");
            }
        }

        // ==============================================================
        // 5. الإضافة السريعة بالنسخ واللصق
        // ==============================================================
        [HttpPost("bulk-quick-import")]
        public async Task<IActionResult> BulkQuickImport([FromBody] BulkImportRequestDto request)
        {
            if (string.IsNullOrWhiteSpace(request?.RawData))
                return BadRequest("لا توجد بيانات للصق.");

            var lines = request.RawData.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);
            if (!lines.Any())
                return BadRequest("النص المدخل فارغ.");

            int addedCount = 0;
            int updatedCount = 0;

            try
            {
                foreach (var line in lines)
                {
                    string[] parts = line.Split('\t');
                    if (parts.Length < 2) parts = line.Split('|');
                    if (parts.Length < 2) parts = line.Split(',');

                    if (parts.Length < 2) continue;

                    string itemName = parts[0].Trim();
                    string itemCode = parts[1].Trim();
                    string departmentName = parts.Length > 2 ? parts[2].Trim() : string.Empty;
                    string variantName = parts.Length > 3 ? parts[3].Trim() : string.Empty;
                    string unit = parts.Length > 4 ? parts[4].Trim() : "قطعة";

                    decimal conversionFactor = 1.0m;
                    if (parts.Length > 5 && decimal.TryParse(parts[5].Trim(), out var parsedFactor))
                        conversionFactor = parsedFactor;

                    decimal price = 0;
                    if (parts.Length > 6 && decimal.TryParse(parts[6].Trim(), out var parsedPrice))
                        price = parsedPrice;

                    string barcode = parts.Length > 7 ? parts[7].Trim() : string.Empty;

                    var existingItem = await _context.Items.FirstOrDefaultAsync(i => i.ItemCode == itemCode);

                    if (existingItem != null)
                    {
                        existingItem.ItemName = itemName;
                        if (!string.IsNullOrEmpty(departmentName)) existingItem.DepartmentName = departmentName;
                        if (price > 0) existingItem.BasePrice = price;
                        if (!string.IsNullOrEmpty(barcode)) existingItem.Barcode = barcode.Trim();
                        if (!string.IsNullOrEmpty(variantName) && variantName != "عادي") existingItem.IsVariant = true;
                        if (conversionFactor > 1) existingItem.MultiUom = true;

                        updatedCount++;
                    }
                    else
                    {
                        var newItem = new Item
                        {
                            CompanyId = request.CompanyId > 0 ? request.CompanyId : 1,
                            CategoryId = request.DefaultCategoryId > 0 ? request.DefaultCategoryId : 1,
                            ItemCode = itemCode,
                            ItemName = itemName,
                            DepartmentName = departmentName,
                            ItemType = "منتج تام",
                            Unit = string.IsNullOrEmpty(unit) ? "قطعة" : unit,
                            BasePrice = price,
                            Barcode = string.IsNullOrWhiteSpace(barcode) ? null : barcode.Trim(),
                            IsVariant = (!string.IsNullOrEmpty(variantName) && variantName != "عادي"),
                            MultiUom = (conversionFactor > 1),
                            Active = true,
                            HasExpiryDate = true,
                            ShelfLifeDays = 4,
                            CreatedAt = DateTime.UtcNow
                        };

                        _context.Items.Add(newItem);
                        addedCount++;
                    }
                }

                await _context.SaveChangesAsync();
                return Ok(new
                {
                    success = true,
                    message = $"تمت المعالجة بنجاح. أصناف جديدة: {addedCount}، أصناف تم تحديثها: {updatedCount}"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"حدث خطأ أثناء معالجة البيانات: {ex.GetBaseException().Message}");
            }
        }

        // ==============================================================
        // 6. تحميل قالب الإكسيل
        // ==============================================================
        [HttpGet("download-template")]
        public IActionResult DownloadTemplate()
        {
            var csv = "ItemCode,ItemName,ItemType,DepartmentName,Unit,BasePrice,Barcode,ShelfLifeDays,SafetyStock,ReorderLevel,MaxStock\n" +
                      "304001,فطيرة سادة,منتج تام,مخبوزات,قطعة,72.00,,3,10,20,100\n" +
                      "302001,جاتوه اسبيشيال,منتج تام,غربي جاتوه,قطعة,0,,3,50,100,500";
            var bytes = System.Text.Encoding.UTF8.GetBytes(csv);
            return File(bytes, "text/csv; charset=utf-8", "Items_Master_Template.csv");
        }

        // ==============================================================
        // 7. استيراد ملف الإكسيل
        // ==============================================================
        [HttpPost("import-excel")]
        public async Task<IActionResult> ImportExcel([FromForm] IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("يرجى اختيار ملف صالح.");

            await Task.CompletedTask;
            return Ok("تم استلام الملف وجاري معالجته.");
        }
    }
}