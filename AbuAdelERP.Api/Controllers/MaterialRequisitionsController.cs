using Microsoft.AspNetCore.Mvc;
using AbuAdelERP.Api.DTOs;
using AbuAdelERP.Api.Services;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AbuAdelERP.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MaterialRequisitionsController : ControllerBase
    {
        private readonly IMaterialRequisitionService _service;

        public MaterialRequisitionsController(IMaterialRequisitionService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<ActionResult<List<RequisitionDetailDto>>> GetAll([FromQuery] string? status)
        {
            return Ok(await _service.GetAllRequisitionsAsync(status));
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<RequisitionDetailDto>> GetById(long id)
        {
            var result = await _service.GetRequisitionByIdAsync(id);
            if (result == null) return NotFound("طلب الصرف غير موجود.");
            return Ok(result);
        }

        [HttpPost]
        public async Task<ActionResult<RequisitionDetailDto>> Create([FromBody] CreateRequisitionDto dto)
        {
            var result = await _service.CreateRequisitionAsync(dto);
            return Ok(result);
        }

        [HttpGet("fefo-suggestions")]
        public async Task<ActionResult<List<FefoBatchSuggestionDto>>> GetFefo([FromQuery] long itemId, [FromQuery] long warehouseId)
        {
            return Ok(await _service.GetFefoSuggestionsAsync(itemId, warehouseId));
        }

        [HttpPost("dispatch")]
        public async Task<IActionResult> Dispatch([FromBody] DispatchRequisitionDto dto)
        {
            try
            {
                var success = await _service.DispatchRequisitionAsync(dto);
                if (!success) return BadRequest("فشل تنفيذ صرف الخامات.");
                return Ok(true);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"خطأ أثناء الصرف: {ex.InnerException?.Message ?? ex.Message}");
            }
        }

        [HttpPost("receive-line")]
        public async Task<IActionResult> ReceiveLine([FromBody] ReceiveLineItemDto dto)
        {
            try
            {
                var success = await _service.ReceiveLineItemAsync(dto);
                if (!success) return BadRequest("فشل تحديث حالة استلام السطر.");
                return Ok(true);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"خطأ أثناء تسجيل استلام السطر: {ex.InnerException?.Message ?? ex.Message}");
            }
        }

        [HttpPost("resolve-discrepancy")]
        public async Task<IActionResult> ResolveDiscrepancy([FromBody] ResolveDiscrepancyDto dto)
        {
            try
            {
                var success = await _service.ResolveDiscrepancyAsync(dto);
                if (!success) return BadRequest("تعذر العثور على سطر النزاع المطلوب تسويته.");
                return Ok(true);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"خطأ أثناء تسوية الخلاف: {ex.InnerException?.Message ?? ex.Message}");
            }
        }
    }
}