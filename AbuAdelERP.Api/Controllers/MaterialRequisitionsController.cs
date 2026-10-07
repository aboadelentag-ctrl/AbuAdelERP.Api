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
    public class MaterialRequisitionsController : ControllerBase
    {
        private readonly IMaterialRequisitionService _requisitionService;

        public MaterialRequisitionsController(IMaterialRequisitionService requisitionService)
        {
            _requisitionService = requisitionService;
        }

        // 1. القوائم المرجعية
        [HttpGet("lookups")]
        public async Task<ActionResult<RequisitionLookupsDto>> GetLookups()
        {
            try
            {
                var lookups = await _requisitionService.GetLookupsAsync();
                return Ok(lookups);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"خطأ في جلب البيانات المرجعية: {ex.Message}");
            }
        }

        // 2. جلب كافة الطلبات
        [HttpGet]
        public async Task<ActionResult<List<RequisitionDetailDto>>> GetRequisitions([FromQuery] string? status = null)
        {
            try
            {
                var requisitions = await _requisitionService.GetAllRequisitionsAsync(status);
                return Ok(requisitions);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"خطأ في جلب طلبات الصرف: {ex.Message}");
            }
        }

        // 3. جلب طلب محدد
        [HttpGet("{id:long}")]
        public async Task<ActionResult<RequisitionDetailDto>> GetRequisition(long id)
        {
            try
            {
                var req = await _requisitionService.GetRequisitionByIdAsync(id);
                if (req == null) return NotFound("طلب الصرف غير موجود.");
                return Ok(req);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"خطأ في استعلام طلب الصرف: {ex.Message}");
            }
        }

        // 4. إنشاء طلب صرف جديد
        [HttpPost]
        public async Task<ActionResult<RequisitionDetailDto>> CreateRequisition([FromBody] CreateRequisitionDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            try
            {
                var result = await _requisitionService.CreateRequisitionAsync(dto);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"خطأ في إنشاء طلب الصرف: {ex.Message}");
            }
        }

        // 5. مقترحات FEFO
        [HttpGet("fefo-suggestions")]
        public async Task<ActionResult<List<FefoBatchSuggestionDto>>> GetFefoSuggestions([FromQuery] long itemId, [FromQuery] long warehouseId)
        {
            try
            {
                var suggestions = await _requisitionService.GetFefoSuggestionsAsync(itemId, warehouseId);
                return Ok(suggestions);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"خطأ في استخراج تشغيلات FEFO: {ex.Message}");
            }
        }

        // 6. صرف الشحنة من المخازن
        [HttpPost("dispatch")]
        public async Task<IActionResult> DispatchRequisition([FromBody] DispatchRequisitionDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            try
            {
                var success = await _requisitionService.DispatchRequisitionAsync(dto);
                if (!success) return NotFound("طلب الصرف غير موجود.");
                return Ok(true);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"خطأ أثناء صرف الشحنة: {ex.Message}");
            }
        }

        // 7. استلام الصالة والمطابقة
        [HttpPost("receive-line")]
        public async Task<IActionResult> ReceiveLineItem([FromBody] ReceiveLineItemDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            try
            {
                var success = await _requisitionService.ReceiveLineItemAsync(dto);
                if (!success) return NotFound("سطر الصرف غير موجود.");
                return Ok(true);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"خطأ أثناء تسجيل استلام الصالة: {ex.Message}");
            }
        }

        // 8. فض النزاع وتسوية الفروق
        [HttpPost("resolve-discrepancy")]
        public async Task<IActionResult> ResolveDiscrepancy([FromBody] ResolveDiscrepancyDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            try
            {
                var success = await _requisitionService.ResolveDiscrepancyAsync(dto);
                if (!success) return NotFound("سطر النزاع المطلوب تسويته غير موجود.");
                return Ok(true);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"خطأ أثناء تسوية النزاع: {ex.Message}");
            }
        }

        // 9. التقرير الرقابي الشامل
        [HttpGet("audit-lifecycle-report")]
        public async Task<ActionResult<List<RequisitionLifecycleAuditDto>>> GetAuditReport(
            [FromQuery] DateTime? startDate,
            [FromQuery] DateTime? endDate,
            [FromQuery] long? departmentId,
            [FromQuery] long? itemId,
            [FromQuery] string? status)
        {
            try
            {
                var report = await _requisitionService.GetAuditLifecycleReportAsync(startDate, endDate, departmentId, itemId, status);
                return Ok(report);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"خطأ في استخراج التقرير الرقابي: {ex.Message}");
            }
        }
    }
}