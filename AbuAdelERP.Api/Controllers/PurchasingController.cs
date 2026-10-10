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
    public class PurchasingController : ControllerBase
    {
        private readonly IPurchasingService _purchasingService;

        public PurchasingController(IPurchasingService purchasingService)
        {
            _purchasingService = purchasingService;
        }

        // ==============================================================
        // 1. جلب أوامر الشراء المعتمدة الجاهزة للاستلام المخزني الفعلي
        // ==============================================================
        [HttpGet("orders-ready-for-receipt")]
        public async Task<ActionResult<List<PurchaseOrderLookupDto>>> GetOrdersReadyForReceipt()
        {
            try
            {
                var result = await _purchasingService.GetApprovedPurchaseOrdersAsync();
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"خطأ أثناء جلب أوامر الشراء الجاهزة للاستلام: {ex.GetBaseException().Message}");
            }
        }

        // ==============================================================
        // 2. إثبات استلام الخامات وترحيلها للأرصدة المخزنية
        // ==============================================================
        [HttpPost("receive-materials")]
        public async Task<IActionResult> ReceiveMaterials([FromBody] MaterialReceiptDto dto)
        {
            if (dto == null) return BadRequest("بيانات الاستلام غير صالحة.");

            try
            {
                var result = await _purchasingService.ReceiveGoodsAsync(dto);
                if (!result) return BadRequest("تعذر حفظ إذن الاستلام. تأكد من إدخال كميات استلام صالحة.");
                return Ok(new { success = true, message = "تم تسجيل الاستلام وترحيل الأرصدة للمخزن بنجاح." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"خطأ أثناء ترحيل المخزن: {ex.GetBaseException().Message}");
            }
        }
    }
}