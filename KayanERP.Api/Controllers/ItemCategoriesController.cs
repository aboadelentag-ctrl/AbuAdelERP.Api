using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using KayanERP.Api.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KayanERP.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ItemCategoriesController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ItemCategoriesController(AppDbContext context)
        {
            _context = context;
        }

        // ==============================================================
        // 1. جلب كافة التصنيفات
        // ==============================================================
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ItemCategory>>> GetItemCategories()
        {
            try
            {
                return await _context.ItemCategories
                    .AsNoTracking()
                    .OrderBy(c => c.CategoryCode)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"خطأ أثناء جلب التصنيفات: {ex.GetBaseException().Message}");
            }
        }

        // ==============================================================
        // 2. جلب تصنيف محدد
        // ==============================================================
        [HttpGet("{id:long}")]
        public async Task<ActionResult<ItemCategory>> GetItemCategory(long id)
        {
            try
            {
                var category = await _context.ItemCategories.FindAsync(id);
                if (category == null) return NotFound("التصنيف غير موجود.");
                return Ok(category);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"خطأ في استعلام التصنيف: {ex.GetBaseException().Message}");
            }
        }

        // ==============================================================
        // 3. إضافة تصنيف جديد
        // ==============================================================
        [HttpPost]
        public async Task<ActionResult<ItemCategory>> PostItemCategory([FromBody] ItemCategory category)
        {
            if (category == null) return BadRequest("بيانات غير صالحة.");

            if (string.IsNullOrWhiteSpace(category.CategoryCode) || string.IsNullOrWhiteSpace(category.CategoryName))
                return BadRequest("كود واسم التصنيف مطلوبان.");

            category.CategoryCode = category.CategoryCode.Trim();
            category.CategoryName = category.CategoryName.Trim();

            // فحص عدم تكرار كود التصنيف
            var codeExists = await _context.ItemCategories.AnyAsync(c => c.CategoryCode.ToLower() == category.CategoryCode.ToLower());
            if (codeExists)
                return BadRequest($"كود التصنيف [{category.CategoryCode}] مسجل مسبقاً. يرجى اختيار كود آخر.");

            category.CompanyId = category.CompanyId > 0 ? category.CompanyId : 1;
            category.CreatedAt = DateTime.UtcNow;
            category.UpdatedAt = DateTime.UtcNow;

            try
            {
                _context.ItemCategories.Add(category);
                await _context.SaveChangesAsync();
                return CreatedAtAction(nameof(GetItemCategory), new { id = category.Id }, category);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"رفضت قاعدة البيانات الحفظ: {ex.GetBaseException().Message}");
            }
        }

        // ==============================================================
        // 4. تعديل تصنيف قائم
        // ==============================================================
        [HttpPut("{id:long}")]
        public async Task<IActionResult> PutItemCategory(long id, [FromBody] ItemCategory category)
        {
            if (category == null) return BadRequest("بيانات غير صالحة.");

            var existingCategory = await _context.ItemCategories.FindAsync(id);
            if (existingCategory == null) return NotFound("التصنيف غير موجود.");

            var trimmedCode = category.CategoryCode?.Trim() ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(trimmedCode))
            {
                var codeExists = await _context.ItemCategories.AnyAsync(c => c.Id != id && c.CategoryCode.ToLower() == trimmedCode.ToLower());
                if (codeExists)
                    return BadRequest($"كود التصنيف [{trimmedCode}] مسجل مسبقاً لتصنيف آخر.");
            }

            existingCategory.CategoryCode = trimmedCode;
            existingCategory.CategoryName = category.CategoryName?.Trim() ?? existingCategory.CategoryName;
            existingCategory.ItemType = string.IsNullOrWhiteSpace(category.ItemType) ? existingCategory.ItemType : category.ItemType;
            existingCategory.Description = category.Description;
            existingCategory.Active = category.Active;
            existingCategory.UpdatedAt = DateTime.UtcNow;

            if (category.CompanyId > 0) existingCategory.CompanyId = category.CompanyId;

            try
            {
                await _context.SaveChangesAsync();
                return Ok(existingCategory);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"رفضت قاعدة البيانات التعديل: {ex.GetBaseException().Message}");
            }
        }

        // ==============================================================
        // 5. تبديل حالة التصنيف (تفعيل / إيقاف)
        // ==============================================================
        [HttpPatch("{id:long}/toggle-status")]
        public async Task<IActionResult> ToggleStatus(long id)
        {
            try
            {
                var category = await _context.ItemCategories.FindAsync(id);
                if (category == null) return NotFound("التصنيف غير موجود.");

                category.Active = !category.Active;
                category.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                return Ok(new { success = true, active = category.Active });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"خطأ أثناء تحديث الحالة: {ex.GetBaseException().Message}");
            }
        }

        // ==============================================================
        // 6. حذف التصنيف
        // ==============================================================
        [HttpDelete("{id:long}")]
        public async Task<IActionResult> DeleteItemCategory(long id)
        {
            try
            {
                var category = await _context.ItemCategories.FindAsync(id);
                if (category == null) return NotFound("التصنيف غير موجود.");

                // فحص إذا كان هناك أصناف مرتبطة بالتصنيف
                bool hasItems = await _context.Items.AnyAsync(i => i.CategoryId == id);
                if (hasItems)
                {
                    category.Active = false; // تعطيل ناعم للحفاظ على سلامة شجرة الأصناف
                    category.UpdatedAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync();
                    return Ok(new { success = true, message = "تم تعطيل التصنيف بدلاً من حذفه نظراً لوجود أصناف مرتبطة به." });
                }

                _context.ItemCategories.Remove(category);
                await _context.SaveChangesAsync();
                return Ok(new { success = true, message = "تم حذف التصنيف بنجاح." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"خطأ أثناء حذف التصنيف: {ex.GetBaseException().Message}");
            }
        }
    }
}