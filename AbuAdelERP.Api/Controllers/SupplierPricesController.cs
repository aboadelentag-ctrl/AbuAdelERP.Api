using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using AbuAdelERP.Api.DTOs;
using AbuAdelERP.Api.Services;

namespace AbuAdelERP.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SupplierPricesController : ControllerBase
    {
        private readonly ISupplierPriceService _priceService;

        public SupplierPricesController(ISupplierPriceService priceService)
        {
            _priceService = priceService;
        }

        // ==============================================================
        // 1. جلب كافة أسعار التوريد مع الفلترة
        // ==============================================================
        [HttpGet]
        public async Task<ActionResult<List<SupplierItemPriceDto>>> GetAllPrices([FromQuery] long? supplierId = null, [FromQuery] long? itemId = null)
        {
            try
            {
                var list = await _priceService.GetAllPricesAsync(supplierId, itemId);
                return Ok(list);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"خطأ في جلب الأسعار: {ex.GetBaseException().Message}");
            }
        }

        // ==============================================================
        // 2. تسجيل سعر توريد جديد أو تحديث السعر الحالي (Upsert)
        // ==============================================================
        [HttpPost]
        public async Task<IActionResult> AddPrice([FromBody] CreateSupplierPriceDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var result = await _priceService.AddOrUpdatePriceAsync(dto);
                return Ok(new { Message = result.Message, Id = result.Id });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"خطأ أثناء حفظ السعر: {ex.GetBaseException().Message}");
            }
        }

        // ==============================================================
        // 3. تعديل سعر توريد قائم
        // ==============================================================
        [HttpPut("{id:long}")]
        public async Task<IActionResult> UpdatePrice(long id, [FromBody] CreateSupplierPriceDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var success = await _priceService.UpdatePriceAsync(id, dto);
                if (!success)
                    return NotFound("سعر التوريد غير موجود.");

                return Ok(new { Message = "تم تحديث سعر التوريد بنجاح!" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"خطأ أثناء التعديل: {ex.GetBaseException().Message}");
            }
        }

        // ==============================================================
        // 4. حذف سعر توريد
        // ==============================================================
        [HttpDelete("{id:long}")]
        public async Task<IActionResult> DeletePrice(long id)
        {
            try
            {
                var success = await _priceService.DeletePriceAsync(id);
                if (!success)
                    return NotFound("السعر غير موجود.");

                return Ok(new { Message = "تم حذف السعر بنجاح." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"تعذر الحذف: {ex.GetBaseException().Message}");
            }
        }
    }
}