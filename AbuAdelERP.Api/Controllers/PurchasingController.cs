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

        [HttpGet("orders-ready-for-receipt")]
        public async Task<ActionResult<List<PurchaseOrderLookupDto>>> GetOrdersReadyForReceipt()
        {
            try
            {
                // استدعاء الدالة المعتمدة في الإنترفيس
                var result = await _purchasingService.GetApprovedPurchaseOrdersAsync();
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"خطأ أثناء جلب أوامر الشراء: {ex.Message}");
            }
        }

        [HttpPost("receive-materials")]
        public async Task<IActionResult> ReceiveMaterials([FromBody] MaterialReceiptDto dto)
        {
            try
            {
                // استدعاء دالة الاستلام المعتمدة في الإنترفيس
                var result = await _purchasingService.ReceiveGoodsAsync(dto);
                if (!result) return BadRequest("تعذر حفظ إذن الاستلام. تأكد من إدخال كميات صالحة.");
                return Ok(true);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"خطأ أثناء ترحيل المخزن: {ex.InnerException?.Message ?? ex.Message}");
            }
        }
    }
}