using Microsoft.AspNetCore.Mvc;
using AbuAdelERP.Api.DTOs;
using AbuAdelERP.Api.Services;

namespace AbuAdelERP.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PurchaseRequestsController : ControllerBase
    {
        private readonly IPurchaseRequestService _requestService;

        public PurchaseRequestsController(IPurchaseRequestService requestService)
        {
            _requestService = requestService;
        }

        [HttpGet("lookups/items")]
        public async Task<ActionResult<List<PurchaseRequestItemLookupDto>>> GetItemsLookup()
        {
            return Ok(await _requestService.GetItemsLookupAsync());
        }

        [HttpGet("lookups/departments")]
        public async Task<ActionResult<List<PurchaseRequestDepartmentLookupDto>>> GetDepartmentsLookup()
        {
            return Ok(await _requestService.GetDepartmentsLookupAsync());
        }

        [HttpGet("lookups/warehouses")]
        public async Task<ActionResult<List<PurchaseRequestWarehouseLookupDto>>> GetWarehousesLookup()
        {
            return Ok(await _requestService.GetWarehousesLookupAsync());
        }

        [HttpGet]
        public async Task<ActionResult<List<PurchaseRequestDto>>> GetAllRequests()
        {
            return Ok(await _requestService.GetAllRequestsAsync());
        }

        [HttpPost]
        public async Task<IActionResult> CreateRequest([FromBody] PurchaseRequestCreateDto dto)
        {
            try
            {
                var result = await _requestService.CreateRequestAsync(dto);
                if (!result) return BadRequest("تعذر حفظ طلب الشراء الداخلي. تأكد من إضافة الأصناف.");
                return Ok(true);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"خطأ في قاعدة البيانات: {ex.InnerException?.Message ?? ex.Message}");
            }
        }
    }
}