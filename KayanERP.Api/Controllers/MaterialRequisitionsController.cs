using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using KayanERP.Api.DTOs;
using KayanERP.Api.Services;

namespace KayanERP.Api.Controllers
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

        // ==============================================================
        // 1. القوائم المرجعية (أقسام، مستودعات، خامات)
        // ==============================================================
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
                return StatusCode(500, $"خطأ في جلب البيانات المرجعية: {ex.GetBaseException().Message}");
            }
        }

        // ==============================================================
        // 2. جلب كافة طلبات الصرف مع إمكانية الفلترة بالحالة
        // ==============================================================
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
                return StatusCode(500, $"خطأ في جلب طلبات الصرف: {ex.GetBaseException().Message}");
            }
        }

        // ==============================================================
        // 3. جلب تفاصيل طلب محدد
        // ==============================================================
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
                return StatusCode(500, $"خطأ في استعلام طلب الصرف: {ex.GetBaseException().Message}");
            }
        }

        // ==============================================================
        // 4. إنشاء طلب صرف جديد من صالة الإنتاج
        // ==============================================================
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
                return StatusCode(500, $"خطأ في إنشاء طلب الصرف: {ex.GetBaseException().Message}");
            }
        }

        // ==============================================================
        // 5. مقترحات الصرف بنظام FEFO (الأقرب انتهاءً أولاً)
        // ==============================================================
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
                return StatusCode(500, $"خطأ في استخراج تشغيلات FEFO: {ex.GetBaseException().Message}");
            }
        }

        // ==============================================================
        // 6. صرف الشحنة من المخازن وتخصيص اللوطات
        // ==============================================================
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
                return StatusCode(500, $"خطأ أثناء صرف الشحنة: {ex.GetBaseException().Message}");
            }
        }

        // ==============================================================
        // 7. استلام الصالة والمطابقة المزدوجة صنفاً بصنف
        // ==============================================================
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
                return StatusCode(500, $"خطأ أثناء تسجيل استلام الصالة: {ex.GetBaseException().Message}");
            }
        }

        // ==============================================================
        // 8. فض النزاع وتسوية الفروق المخزنية
        // ==============================================================
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
                return StatusCode(500, $"خطأ أثناء تسوية النزاع: {ex.GetBaseException().Message}");
            }
        }

        // ==============================================================
        // 9. التقرير الرقابي الشامل لدورة حياة الخامات وأرصدة الصالات
        // ==============================================================
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
                return StatusCode(500, $"خطأ في استخراج التقرير الرقابي: {ex.GetBaseException().Message}");
            }
        }
    }
}