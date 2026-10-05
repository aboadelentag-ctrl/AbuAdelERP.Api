using Microsoft.AspNetCore.Mvc;
using AbuAdelERP.Api.DTOs;
using AbuAdelERP.Api.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AbuAdelERP.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RecipesController : ControllerBase
    {
        private readonly IRecipeService _recipeService;

        public RecipesController(IRecipeService recipeService)
        {
            _recipeService = recipeService;
        }

        // ==============================================================
        // 1. استعراض كافة وصفات ومعايير التصنيع
        // ==============================================================
        [HttpGet]
        public async Task<ActionResult<List<RecipeDto>>> GetRecipes()
        {
            try
            {
                var result = await _recipeService.GetAllRecipesAsync();
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"خطأ أثناء جلب الوصفات: {ex.Message}");
            }
        }

        // ==============================================================
        // 2. جلب تفاصيل بطاقة معيار خلطة محددة
        // ==============================================================
        [HttpGet("{id}")]
        public async Task<ActionResult<RecipeDto>> GetRecipe(long id)
        {
            try
            {
                var result = await _recipeService.GetRecipeByIdAsync(id);
                if (result == null) return NotFound("الوصفة غير موجودة.");
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"خطأ أثناء جلب الوصفة: {ex.Message}");
            }
        }

        // ==============================================================
        // 3. إنشاء أو تعديل معيار خلطة
        // ==============================================================
        [HttpPost]
        public async Task<IActionResult> SaveRecipe([FromBody] SaveRecipeDto dto)
        {
            try
            {
                var success = await _recipeService.SaveRecipeAsync(dto);
                if (!success) return BadRequest("تعذر حفظ الوصفة. تأكد من تحديد المنتج والخامات ومقاديرها.");
                return Ok(true);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"خطأ في الحفظ: {ex.InnerException?.Message ?? ex.Message}");
            }
        }

        // ==============================================================
        // 4. استيراد معايير وخلطات التصنيع دفعة واحدة من الإكسيل (Bulk Import)
        // ==============================================================
        [HttpPost("bulk-import")]
        public async Task<IActionResult> BulkImportRecipes([FromBody] List<SaveRecipeDto> dtoList)
        {
            if (dtoList == null || !dtoList.Any())
                return BadRequest("لا توجد وصفات للاستيراد.");

            try
            {
                int count = await _recipeService.BulkImportRecipesAsync(dtoList);
                return Ok(count);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"خطأ أثناء استيراد الوصفات: {ex.GetBaseException().Message}");
            }
        }

        // ==============================================================
        // 5. حذف معيار خلطة
        // ==============================================================
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteRecipe(long id)
        {
            try
            {
                var success = await _recipeService.DeleteRecipeAsync(id);
                if (!success) return NotFound("الوصفة غير موجودة أو تعذر حذفها.");
                return Ok(true);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"خطأ في الحذف: {ex.Message}");
            }
        }
    }
}