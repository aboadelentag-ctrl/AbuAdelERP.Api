using Microsoft.AspNetCore.Mvc;
using KayanERP.Api.DTOs;
using KayanERP.Api.Services;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace KayanERP.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductionOperationsController : ControllerBase
    {
        private readonly IProductionOperationsService _operationsService;

        public ProductionOperationsController(IProductionOperationsService operationsService)
        {
            _operationsService = operationsService;
        }

        // ========================================================
        // 1. الدورة المغلقة: سحب صالات الإنتاج ومخازنها وعمالها
        // ========================================================
        [HttpGet("lookups")]
        public async Task<ActionResult<ProductionOperationsLookupsDto>> GetInitialLookups()
        {
            try
            {
                var result = await _operationsService.GetInitialLookupsAsync();
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"خطأ في جلب بيانات صالات الإنتاج: {ex.GetBaseException().Message}");
            }
        }

        // ========================================================
        // 2. أوامر التشغيل المفتوحة
        // ========================================================
        [HttpGet("active-orders")]
        public async Task<ActionResult<List<ActiveProductionOrderDto>>> GetActiveOrders()
        {
            try
            {
                var orders = await _operationsService.GetActiveOrdersAsync();
                return Ok(orders);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"خطأ في جلب أوامر التشغيل: {ex.GetBaseException().Message}");
            }
        }

        [HttpPost("create-order")]
        public async Task<IActionResult> CreateOrder([FromBody] CreateBatchOrderDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var result = await _operationsService.CreateOrderAsync(dto);
                if (!result.Success)
                    return BadRequest(result.Message);

                return Ok(new { message = result.Message, orderId = result.OrderId, orderNumber = result.OrderNumber });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"فشل إصدار أمر التشغيل: {ex.GetBaseException().Message}");
            }
        }

        // ========================================================
        // 3. التنفيذ الميداني وتقفيل كامل بنود الأمر
        // ========================================================
        [HttpGet("order-execution-full/{orderId:long}")]
        public async Task<ActionResult<OrderExecutionFullDto>> GetOrderExecutionFull(long orderId)
        {
            try
            {
                var result = await _operationsService.GetOrderExecutionFullAsync(orderId);
                if (result == null)
                    return NotFound("أمر التشغيل غير موجود.");

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"خطأ في استخراج بيانات التنفيذ: {ex.GetBaseException().Message}");
            }
        }

        [HttpPost("finalize-order-complete")]
        public async Task<IActionResult> FinalizeOrderComplete([FromBody] CompleteOrderFinalizeDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var success = await _operationsService.FinalizeOrderCompleteAsync(dto);
                if (!success)
                    return NotFound("أمر التشغيل غير موجود.");

                return Ok(new { message = "تم تقفيل واعتماد أمر التشغيل بالكامل وترصيد اللوطات والهالك ✓" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"خطأ في تقفيل أمر التشغيل: {ex.GetBaseException().Message}");
            }
        }

        // ========================================================
        // 4. ترصيد إنتاجية الوردية وحفظ الحضور وساعات العمل
        // ========================================================
        [HttpPost("submit-shift-productivity")]
        public async Task<IActionResult> SubmitShiftProductivity([FromBody] ShiftProductivityDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var message = await _operationsService.SubmitShiftProductivityAsync(dto);
                return Ok(new { message });
            }
            catch (Exception ex)
            {
                var detailedError = ex.InnerException?.InnerException?.Message
                                 ?? ex.InnerException?.Message
                                 ?? ex.GetBaseException().Message;
                return StatusCode(500, $"خطأ في تسجيل إنتاجية الوردية: {detailedError}");
            }
        }

        // ========================================================
        // 5. مركز التقارير واليوميات ومؤشرات كفاءة الصالات
        // ========================================================
        [HttpGet("kpis")]
        public async Task<ActionResult<List<DepartmentKpiDto>>> GetDepartmentKPIs([FromQuery] DateTime startDate, [FromQuery] DateTime endDate, [FromQuery] long? departmentId)
        {
            try
            {
                var kpis = await _operationsService.GetDepartmentKPIsAsync(startDate, endDate, departmentId);
                return Ok(kpis);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"خطأ في استخراج مؤشرات الأداء: {ex.GetBaseException().Message}");
            }
        }

        [HttpGet("detailed-attendance-logs")]
        public async Task<ActionResult<List<DetailedAttendanceLogDto>>> GetDetailedAttendanceLogs([FromQuery] DateTime startDate, [FromQuery] DateTime endDate, [FromQuery] long? departmentId)
        {
            try
            {
                var logs = await _operationsService.GetDetailedAttendanceLogsAsync(startDate, endDate, departmentId);
                return Ok(logs);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"خطأ في جلب يوميات الحضور والإنتاج: {ex.GetBaseException().Message}");
            }
        }

        [HttpGet("employee-dossier/{employeeId:long}")]
        public async Task<ActionResult<EmployeeDossierDto>> GetEmployeeDossier(long employeeId)
        {
            try
            {
                var dossier = await _operationsService.GetEmployeeDossierAsync(employeeId);
                if (dossier == null)
                    return NotFound("الموظف غير موجود.");

                return Ok(dossier);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"خطأ في جلب ملف الموظف: {ex.GetBaseException().Message}");
            }
        }

        [HttpPost("calculate-cost")]
        public async Task<ActionResult<BatchCostCalculationResultDto>> CalculateBatchCost([FromBody] BatchCostCalculationRequestDto req)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var result = await _operationsService.CalculateBatchCostAsync(req);
                if (result == null)
                    return NotFound("أمر التشغيل غير موجود.");

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"خطأ في حساب التكلفة: {ex.GetBaseException().Message}");
            }
        }
    }
}