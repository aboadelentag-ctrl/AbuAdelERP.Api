using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using KayanERP.Shared.DTOs;
using KayanERP.Api.Services;

namespace KayanERP.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CratesController : ControllerBase
    {
        private readonly ICrateService _crateService;

        public CratesController(ICrateService crateService)
        {
            _crateService = crateService;
        }

        // ========================================================
        // 1. القوائم المرجعية
        // ========================================================
        [HttpGet("lookups")]
        public async Task<ActionResult<CrateLookupsDto>> GetLookups()
        {
            try
            {
                var result = await _crateService.GetLookupsAsync();
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"خطأ في جلب بيانات الحاويات المرجعية: {ex.GetBaseException().Message}");
            }
        }

        // ========================================================
        // 2. أرصدة الحاويات اللحظية
        // ========================================================
        [HttpGet("inventory")]
        public async Task<ActionResult<List<CirculatingContainerViewDto>>> GetInventory()
        {
            try
            {
                var inventory = await _crateService.GetInventoryAsync();
                return Ok(inventory);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"خطأ في جلب أرصدة الحاويات: {ex.GetBaseException().Message}");
            }
        }

        // ========================================================
        // 3. سجل الحركات
        // ========================================================
        [HttpGet("movements")]
        public async Task<ActionResult<List<CrateMovementViewDto>>> GetMovements([FromQuery] string? status)
        {
            try
            {
                var list = await _crateService.GetMovementsAsync(status);
                return Ok(list);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"خطأ في جلب حركات الحاويات: {ex.GetBaseException().Message}");
            }
        }

        // ========================================================
        // 4. صرف الحاويات (طرف أول)
        // ========================================================
        [HttpPost("dispatch")]
        public async Task<IActionResult> Dispatch([FromBody] DispatchCrateDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var fallbackCode = User.FindFirstValue("EmployeeCode") ?? "EMP-088";
                var fallbackName = User.FindFirstValue(ClaimTypes.Name) ?? "أمين المستودع";

                var result = await _crateService.DispatchCratesAsync(dto, fallbackCode, fallbackName);
                return Ok(new { message = "تم إصدار إذن التسليم بنجاح، بانتظار تأكيد المستلم", movement = result });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"خطأ أثناء صرف الحاويات: {ex.GetBaseException().Message}");
            }
        }

        // ========================================================
        // 5. تأكيد الاستلام الميداني (طرف ثانٍ)
        // ========================================================
        [HttpPost("movements/{id:long}/receive")]
        public async Task<IActionResult> Receive(long id, [FromBody] ReceiveCrateDto dto)
        {
            try
            {
                var fallbackCode = User.FindFirstValue("EmployeeCode") ?? "EMP-045";
                var fallbackName = User.FindFirstValue(ClaimTypes.Name) ?? "المستلم الميداني";

                var (success, message, movement) = await _crateService.ReceiveCratesAsync(id, dto, fallbackCode, fallbackName);
                if (!success)
                    return BadRequest(message);

                return Ok(new { message, movement });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"خطأ أثناء معالجة الاستلام: {ex.GetBaseException().Message}");
            }
        }

        // ========================================================
        // 6. فض وتسوية النزاع
        // ========================================================
        [HttpPost("movements/{id:long}/settle-dispute")]
        public async Task<IActionResult> SettleDispute(long id, [FromBody] SettleDisputeDto dto)
        {
            try
            {
                var fallbackCode = User.FindFirstValue("EmployeeCode") ?? "ADMIN-01";
                var fallbackName = User.FindFirstValue(ClaimTypes.Name) ?? "رئيس لجنة التحكيم";

                var (success, message, movement) = await _crateService.SettleDisputeAsync(id, dto, fallbackCode, fallbackName);
                if (!success)
                    return BadRequest(message);

                return Ok(new { message, movement });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"خطأ أثناء تسوية النزاع: {ex.GetBaseException().Message}");
            }
        }
    }
}