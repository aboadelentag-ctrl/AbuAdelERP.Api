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
    public class VendorQcController : ControllerBase
    {
        private readonly IVendorQcService _vendorQcService;

        public VendorQcController(IVendorQcService vendorQcService)
        {
            _vendorQcService = vendorQcService;
        }

        [HttpGet("ratings")]
        public async Task<ActionResult<List<VendorQcScoreDto>>> GetRatings([FromQuery] DateTime? fromDate, [FromQuery] DateTime? toDate)
        {
            try
            {
                var data = await _vendorQcService.GetVendorRatingsAsync(fromDate, toDate);
                return Ok(data);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"خطأ في استخراج تقييمات الموردين: {ex.GetBaseException().Message}");
            }
        }

        [HttpGet("history")]
        public async Task<ActionResult<List<QcInspectionHistoryDto>>> GetHistory([FromQuery] DateTime? fromDate, [FromQuery] DateTime? toDate)
        {
            try
            {
                var data = await _vendorQcService.GetInspectionHistoryAsync(fromDate, toDate);
                return Ok(data);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"خطأ في جلب سجل فحص الجودة: {ex.GetBaseException().Message}");
            }
        }
    }
}