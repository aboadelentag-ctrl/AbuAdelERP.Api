using Microsoft.AspNetCore.Mvc;
using KayanERP.Api.DTOs;
using KayanERP.Api.Services;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace KayanERP.Api.Controllers
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

        // ==============================================================
        // 1. معاملات تحويل الوحدات (UoM Conversions)
        // ==============================================================
        [HttpGet("conversions")]
        public async Task<ActionResult<List<UomConversionDto>>> GetConversions()
        {
            try
            {
                var result = await _hierarchyService.GetAllConversionsAsync();
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"خطأ في جلب معاملات التحويل: {ex.GetBaseException().Message}");
            }
        }

        [HttpPost("conversions")]
        public async Task<IActionResult> CreateConversion([FromBody] UomConversionCreateDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            try
            {
                var result = await _hierarchyService.CreateConversionAsync(dto);
                if (!result) return BadRequest("بيانات معامل التحويل غير صحيحة أو المعامل يساوي صفراً.");
                return Ok(true);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"خطأ في حفظ معامل التحويل: {ex.GetBaseException().Message}");
            }
        }

        // ==============================================================
        // 2. الأصناف الأساسية (Parents Lookup)
        // ==============================================================
        [HttpGet("parents")]
        public async Task<ActionResult<List<ParentItemLookupDto>>> GetParentItems()
        {
            try
            {
                var parents = await _hierarchyService.GetParentItemsLookupAsync();
                return Ok(parents);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"خطأ في جلب الأصناف الأساسية: {ex.GetBaseException().Message}");
            }
        }

        // ==============================================================
        // 3. تفريعات الأصناف والنكهات (Variants)
        // ==============================================================
        [HttpGet("variants")]
        public async Task<ActionResult<List<ItemVariantDto>>> GetAllVariants()
        {
            try
            {
                var variants = await _hierarchyService.GetAllVariantsAsync();
                return Ok(variants);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"خطأ في جلب تفريعات الأصناف: {ex.GetBaseException().Message}");
            }
        }

        [HttpPost("variants")]
        public async Task<IActionResult> CreateVariant([FromBody] ItemVariantCreateDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            try
            {
                var result = await _hierarchyService.CreateItemVariantAsync(dto);
                if (!result) return BadRequest("تعذر إنشاء تفريع الصنف. تأكد من تحديد الصنف الأساسي وكتابة اسم التفرع.");
                return Ok(true);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"خطأ في إنشاء تفرع الصنف: {ex.GetBaseException().Message}");
            }
        }
    }
}