using Microsoft.AspNetCore.Mvc;
using AbuAdelERP.Api.DTOs;
using AbuAdelERP.Api.Services;

namespace AbuAdelERP.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ItemHierarchyController : ControllerBase
    {
        private readonly IItemHierarchyService _hierarchyService;

        public ItemHierarchyController(IItemHierarchyService hierarchyService)
        {
            _hierarchyService = hierarchyService;
        }

        [HttpGet("conversions")]
        public async Task<ActionResult<List<UomConversionDto>>> GetConversions()
        {
            try
            {
                return Ok(await _hierarchyService.GetAllConversionsAsync());
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"خطأ في جلب التحويلات: {ex.InnerException?.Message ?? ex.Message}");
            }
        }

        [HttpPost("conversions")]
        public async Task<IActionResult> CreateConversion([FromBody] UomConversionCreateDto dto)
        {
            try
            {
                var result = await _hierarchyService.CreateConversionAsync(dto);
                if (!result) return BadRequest("بيانات معامل التحويل غير صحيحة.");
                return Ok(true);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"خطأ في قاعدة البيانات: {ex.InnerException?.Message ?? ex.Message}");
            }
        }

        [HttpGet("parents")]
        public async Task<ActionResult<List<ParentItemLookupDto>>> GetParentItems()
        {
            try
            {
                return Ok(await _hierarchyService.GetParentItemsLookupAsync());
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"خطأ في جلب الأصناف: {ex.InnerException?.Message ?? ex.Message}");
            }
        }

        [HttpGet("variants")]
        public async Task<ActionResult<List<ItemVariantDto>>> GetAllVariants()
        {
            try
            {
                return Ok(await _hierarchyService.GetAllVariantsAsync());
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"خطأ في جلب التفريعات: {ex.InnerException?.Message ?? ex.Message}");
            }
        }

        [HttpPost("variants")]
        public async Task<IActionResult> CreateVariant([FromBody] ItemVariantCreateDto dto)
        {
            try
            {
                var result = await _hierarchyService.CreateItemVariantAsync(dto);
                if (!result) return BadRequest("تعذر إنشاء تفريع الصنف. تأكد من تحديد الصنف الأساسي.");
                return Ok(true);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"خطأ في قاعدة البيانات: {ex.InnerException?.Message ?? ex.Message}");
            }
        }
    }
}