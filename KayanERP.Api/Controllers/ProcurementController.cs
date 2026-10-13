using Microsoft.AspNetCore.Mvc;
using KayanERP.Api.DTOs;
using KayanERP.Api.Services;

namespace KayanERP.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProcurementController : ControllerBase
    {
        private readonly IProcurementService _procurementService;

        public ProcurementController(IProcurementService procurementService)
        {
            _procurementService = procurementService;
        }

        [HttpGet("pending-requests")]
        public async Task<ActionResult<List<ProcurementPendingRequestDto>>> GetPendingRequests()
        {
            try
            {
                var result = await _procurementService.GetPendingRequestsAsync();
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"خطأ: {ex.Message}");
            }
        }

        [HttpGet("pending-requests/{id}")]
        public async Task<ActionResult<ProcurementPendingRequestDto>> GetRequestDetails(long id)
        {
            try
            {
                var result = await _procurementService.GetRequestDetailsAsync(id);
                if (result == null) return NotFound("طلب الاحتياج غير موجود.");
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"خطأ: {ex.Message}");
            }
        }

        [HttpPost("convert-to-invoice")]
        public async Task<IActionResult> ConvertToInvoice([FromBody] ProcurementConvertDto dto)
        {
            try
            {
                var result = await _procurementService.ConvertToApprovedInvoiceAsync(dto);
                if (!result) return BadRequest("تعذر تحويل الطلب لفاتورة. تأكد من تحديد المورد وبنود الأسعار.");
                return Ok(true);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"خطأ في الاعتماد: {ex.InnerException?.Message ?? ex.Message}");
            }
        }

        [HttpGet("approved-invoices")]
        public async Task<ActionResult<List<ProcurementApprovedInvoiceDto>>> GetApprovedInvoices()
        {
            try
            {
                var result = await _procurementService.GetApprovedInvoicesAsync();
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"خطأ: {ex.Message}");
            }
        }
    }
}