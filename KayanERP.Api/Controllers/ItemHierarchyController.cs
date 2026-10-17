using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using KayanERP.Shared.DTOs;
using KayanERP.Api.Services;

namespace KayanERP.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ItemHierarchyController : ControllerBase
    {
        private readonly IItemHierarchyService _hierarchyService;

        public ItemHierarchyController(IItemHierarchyService hierarchyService)
        {
            _hierarchyService = hierarchyService;
        }

        // ==============================================================
        // 1. معاملات تحويل الوحدات (UoM Conversions)
        // ==============================================================
        [HttpGet("conversions")]
        public async Task<ActionResult<List<UomConversionDto>>> GetConversions()
        {
            var result = await _hierarchyService.GetAllConversionsAsync();
            return Ok(result);
        }

        [HttpPost("conversions")]
        public async Task<IActionResult> CreateConversion([FromBody] UomConversionCreateDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _hierarchyService.CreateConversionAsync(dto);

            if (!result)
                return BadRequest(new { message = "بيانات معامل التحويل غير صحيحة أو المعامل يساوي صفراً." });

            return Ok(new { success = true });
        }

        // ==============================================================
        // 2. الأصناف الأساسية (Parents Lookup)
        // ==============================================================
        [HttpGet("parents")]
        public async Task<ActionResult<List<ParentItemLookupDto>>> GetParentItems()
        {
            var parents = await _hierarchyService.GetParentItemsLookupAsync();
            return Ok(parents);
        }

        // ==============================================================
        // 3. تفريعات الأصناف والنكهات (Variants)
        // ==============================================================
        [HttpGet("variants")]
        public async Task<ActionResult<List<ItemVariantDto>>> GetAllVariants()
        {
            var variants = await _hierarchyService.GetAllVariantsAsync();
            return Ok(variants);
        }

        [HttpPost("variants")]
        public async Task<IActionResult> CreateVariant([FromBody] ItemVariantCreateDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _hierarchyService.CreateItemVariantAsync(dto);

            if (!result)
                return BadRequest(new { message = "تعذر إنشاء تفريع الصنف. تأكد من تحديد الصنف الأساسي وكتابة اسم التفرع." });

            return Ok(new { success = true });
        }
    }
}