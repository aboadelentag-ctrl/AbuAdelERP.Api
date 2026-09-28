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
                return StatusCode(500, $"خطأ: {ex.Message}");
            }
        }

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
        // مسار استيراد معايير وخلطات التصنيع دفعة واحدة من الإكسيل
        // ==============================================================
        [HttpPost("bulk-import")]
        public async Task<IActionResult> BulkImportRecipes([FromBody] List<SaveRecipeDto> dtoList)
        {
            if (dtoList == null || !dtoList.Any())
                return BadRequest("لا توجد وصفات للاستيراد.");

            try
            {
                int count = 0;
                foreach (var dto in dtoList)
                {
                    var success = await _recipeService.SaveRecipeAsync(dto);
                    if (success) count++;
                }

                return Ok(new
                {
                    success = true,
                    count = count,
                    message = $"تم استيراد وحفظ وتحديث ({count}) بطاقة معيار خلطة بنجاح!"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"خطأ أثناء استيراد الوصفات: {ex.GetBaseException().Message}");
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteRecipe(long id)
        {
            try
            {
                var success = await _recipeService.DeleteRecipeAsync(id);
                if (!success) return NotFound();
                return Ok(true);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"خطأ في الحذف: {ex.Message}");
            }
        }
    }
}