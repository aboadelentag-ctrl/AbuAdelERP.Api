using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AbuAdelERP.Api.DTOs;
using AbuAdelERP.Api.Services;

namespace AbuAdelERP.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class QualityController : ControllerBase
    {
        private readonly IQualityService _qualityService;

        public QualityController(IQualityService qualityService)
        {
            _qualityService = qualityService;
        }

        // ==============================================================
        // 1. استعراض كافة تقارير الجودة المسجلة
        // ==============================================================
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            try
            {
                var reports = await _qualityService.GetAllInspectionsAsync();
                return Ok(reports);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"خطأ أثناء جلب تقارير الجودة: {ex.GetBaseException().Message}");
            }
        }

        // ==============================================================
        // 2. جلب أوامر التشغيل أو الشحنات بانتظار الفحص
        // ==============================================================
        [HttpGet("pending-orders")]
        public async Task<IActionResult> GetPendingOrders()
        {
            try
            {
                var orders = await _qualityService.GetPendingOrdersForInspectionAsync();
                return Ok(orders);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"خطأ أثناء استعلام الأوامر المعلقة: {ex.GetBaseException().Message}");
            }
        }

        // ==============================================================
        // 3. اعتماد تسجيل فحص الجودة وتطبيق القرار الآلي
        // ==============================================================
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] QualityInspectionCreateDto request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var result = await _qualityService.SubmitInspectionAsync(request);
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Error = $"خطأ أثناء تسجيل فحص الجودة: {ex.GetBaseException().Message}" });
            }
        }
    }
}