using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AbuAdelERP.Api.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AbuAdelERP.Api.Controllers
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

        [HttpGet]
        public async Task<ActionResult<IEnumerable<ItemCategory>>> GetItemCategories()
        {
            return await _context.ItemCategories.OrderByDescending(c => c.Id).ToListAsync();
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ItemCategory>> GetItemCategory(long id)
        {
            var category = await _context.ItemCategories.FindAsync(id);
            if (category == null) return NotFound();
            return category;
        }

        [HttpPost]
        public async Task<ActionResult<ItemCategory>> PostItemCategory([FromBody] ItemCategory category)
        {
            category.CreatedAt = DateTime.UtcNow;
            category.UpdatedAt = DateTime.UtcNow;

            // لضمان عدم حدوث خطأ إذا لم يتم إرسال رقم الشركة
            if (category.CompanyId == 0) category.CompanyId = 1;

            try
            {
                _context.ItemCategories.Add(category);
                await _context.SaveChangesAsync();
                return CreatedAtAction(nameof(GetItemCategory), new { id = category.Id }, category);
            }
            catch (DbUpdateException ex)
            {
                var innerMessage = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                return BadRequest($"رفضت قاعدة البيانات الحفظ: {innerMessage}");
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> PutItemCategory(long id, [FromBody] ItemCategory category)
        {
            if (id != category.Id) return BadRequest("معرف التصنيف غير متطابق.");

            var existingCategory = await _context.ItemCategories.FindAsync(id);
            if (existingCategory == null) return NotFound("التصنيف غير موجود.");

            existingCategory.CategoryCode = category.CategoryCode;
            existingCategory.CategoryName = category.CategoryName;
            existingCategory.ItemType = category.ItemType;
            existingCategory.Description = category.Description;
            existingCategory.Active = category.Active;
            existingCategory.UpdatedAt = DateTime.UtcNow;

            if (category.CompanyId > 0) existingCategory.CompanyId = category.CompanyId;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                var innerMessage = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                return BadRequest($"رفضت قاعدة البيانات التعديل: {innerMessage}");
            }

            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteItemCategory(long id)
        {
            var category = await _context.ItemCategories.FindAsync(id);
            if (category == null) return NotFound();

            category.Active = false; // الإيقاف بدلاً من الحذف
            category.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}