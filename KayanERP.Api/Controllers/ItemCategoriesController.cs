using System;
using System.Collections.Generic;
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
    public class ItemCategoriesController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ITenantContext _tenantContext;

        public ItemCategoriesController(AppDbContext context, ITenantContext tenantContext)
        {
            _context = context;
            _tenantContext = tenantContext;
        }

        // ==============================================================
        // 1. جلب كافة التصنيفات (مفلترة تلقائيًا بواسطة Global Query Filter)
        // ==============================================================
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ItemCategoryDto>>> GetItemCategories()
        {
            var categories = await _context.ItemCategories
                .AsNoTracking()
                .OrderBy(c => c.CategoryCode)
                .Select(c => new ItemCategoryDto
                {
                    Id = c.Id,
                    CompanyId = c.CompanyId,
                    CategoryCode = c.CategoryCode,
                    CategoryName = c.CategoryName,
                    ItemType = c.ItemType,
                    Description = c.Description,
                    Active = c.Active,
                    CreatedAt = c.CreatedAt,
                    UpdatedAt = c.UpdatedAt
                })
                .ToListAsync();

            return Ok(categories);
        }

        // ==============================================================
        // 2. جلب تصنيف محدد
        // ==============================================================
        [HttpGet("{id:long}")]
        public async Task<ActionResult<ItemCategoryDto>> GetItemCategory(long id)
        {
            var category = await _context.ItemCategories
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == id);

            if (category == null)
                return NotFound(new { message = "التصنيف غير موجود." });

            return Ok(new ItemCategoryDto
            {
                Id = category.Id,
                CompanyId = category.CompanyId,
                CategoryCode = category.CategoryCode,
                CategoryName = category.CategoryName,
                ItemType = category.ItemType,
                Description = category.Description,
                Active = category.Active,
                CreatedAt = category.CreatedAt,
                UpdatedAt = category.UpdatedAt
            });
        }

        // ==============================================================
        // 3. إضافة تصنيف جديد
        // ==============================================================
        [HttpPost]
        public async Task<ActionResult<ItemCategoryDto>> PostItemCategory([FromBody] CreateItemCategoryDto dto)
        {
            if (!ModelState.IsValid ||
                string.IsNullOrWhiteSpace(dto.CategoryCode) ||
                string.IsNullOrWhiteSpace(dto.CategoryName))
                return BadRequest(new { message = "كود واسم التصنيف مطلوبان." });

            var companyId = _tenantContext.CompanyId;
            var code = dto.CategoryCode.Trim();
            var name = dto.CategoryName.Trim();

            // فحص التكرار داخل نفس الشركة
            var codeExists = await _context.ItemCategories
                .AnyAsync(c => c.CompanyId == companyId &&
                               c.CategoryCode.ToLower() == code.ToLower());

            if (codeExists)
                return Conflict(new { message = $"كود التصنيف [{code}] مسجل مسبقاً." });

            var category = new ItemCategory
            {
                CompanyId = companyId,
                CategoryCode = code,
                CategoryName = name,
                ItemType = dto.ItemType?.Trim() ?? string.Empty,
                Description = dto.Description?.Trim(),
                Active = dto.Active,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.ItemCategories.Add(category);
            await _context.SaveChangesAsync();

            var result = new ItemCategoryDto
            {
                Id = category.Id,
                CompanyId = category.CompanyId,
                CategoryCode = category.CategoryCode,
                CategoryName = category.CategoryName,
                ItemType = category.ItemType,
                Description = category.Description,
                Active = category.Active,
                CreatedAt = category.CreatedAt,
                UpdatedAt = category.UpdatedAt
            };

            return CreatedAtAction(nameof(GetItemCategory), new { id = category.Id }, result);
        }

        // ==============================================================
        // 4. تعديل تصنيف
        // ==============================================================
        [HttpPut("{id:long}")]
        public async Task<IActionResult> PutItemCategory(long id, [FromBody] CreateItemCategoryDto dto)
        {
            var existingCategory = await _context.ItemCategories.FindAsync(id);
            if (existingCategory == null)
                return NotFound(new { message = "التصنيف غير موجود." });

            // تأكيد ملكية التصنيف للشركة
            if (existingCategory.CompanyId != _tenantContext.CompanyId)
                return Forbid();

            var trimmedCode = dto.CategoryCode?.Trim() ?? string.Empty;
            var trimmedName = dto.CategoryName?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(trimmedCode) || string.IsNullOrWhiteSpace(trimmedName))
                return BadRequest(new { message = "كود واسم التصنيف مطلوبان." });

            // فحص التكرار داخل نفس الشركة (استثناء التصنيف الحالي)
            var codeExists = await _context.ItemCategories
                .AnyAsync(c => c.Id != id &&
                               c.CompanyId == existingCategory.CompanyId &&
                               c.CategoryCode.ToLower() == trimmedCode.ToLower());

            if (codeExists)
                return Conflict(new { message = $"كود التصنيف [{trimmedCode}] مسجل مسبقاً لتصنيف آخر." });

            // نحدّث الحقول المسموح بها فقط — نتجاهل CompanyId
            existingCategory.CategoryCode = trimmedCode;
            existingCategory.CategoryName = trimmedName;
            if (!string.IsNullOrWhiteSpace(dto.ItemType))
                existingCategory.ItemType = dto.ItemType.Trim();
            existingCategory.Description = dto.Description?.Trim();
            existingCategory.Active = dto.Active;
            existingCategory.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return NoContent();
        }

        // ==============================================================
        // 5. تبديل حالة التصنيف
        // ==============================================================
        [HttpPatch("{id:long}/toggle-status")]
        public async Task<IActionResult> ToggleStatus(long id)
        {
            var category = await _context.ItemCategories.FindAsync(id);
            if (category == null)
                return NotFound(new { message = "التصنيف غير موجود." });

            if (category.CompanyId != _tenantContext.CompanyId)
                return Forbid();

            category.Active = !category.Active;
            category.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return Ok(new { success = true, active = category.Active });
        }

        // ==============================================================
        // 6. حذف التصنيف (Soft Delete دايمًا)
        // ==============================================================
        [HttpDelete("{id:long}")]
        public async Task<IActionResult> DeleteItemCategory(long id)
        {
            var category = await _context.ItemCategories.FindAsync(id);
            if (category == null)
                return NotFound(new { message = "التصنيف غير موجود." });

            if (category.CompanyId != _tenantContext.CompanyId)
                return Forbid();

            // فحص وجود أصناف مرتبطة
            bool hasItems = await _context.Items.AnyAsync(i => i.CategoryId == id);

            // Soft Delete دايمًا — أأمن من Hard Delete
            category.Active = false;
            category.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            var message = hasItems
                ? "تم تعطيل التصنيف بدلاً من حذفه نظراً لوجود أصناف مرتبطة به."
                : "تم تعطيل التصنيف بنجاح.";

            return Ok(new { success = true, message });
        }
    }
}