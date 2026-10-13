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
    public class FinishedGoodsTransfersController : ControllerBase
    {
        private readonly IFinishedGoodsTransferService _transferService;

        public FinishedGoodsTransfersController(IFinishedGoodsTransferService transferService)
        {
            _transferService = transferService;
        }

        // ========================================================
        // 1. القوائم المرجعية الموحدة (مخازن الصالات ومخازن التام والأصناف)
        // ========================================================
        [HttpGet("lookups")]
        public async Task<ActionResult<TransferLookupsDto>> GetLookups()
        {
            try
            {
                var result = await _transferService.GetLookupsAsync();
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"خطأ في جلب البيانات المرجعية للمنتج التام: {ex.Message}");
            }
        }

        // ========================================================
        // 2. استعراض سجل أذونات تحويل المنتج التام
        // ========================================================
        [HttpGet]
        public async Task<ActionResult<List<FinishedGoodsTransferViewDto>>> GetAll()
        {
            try
            {
                var list = await _transferService.GetAllTransfersAsync();
                return Ok(list);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"خطأ في جلب بيانات التحويلات: {ex.Message}");
            }
        }

        // ========================================================
        // 3. المحطة 1: إصدار إذن تسليم من صالة التشغيل
        // ========================================================
        [HttpPost("create")]
        public async Task<IActionResult> Create([FromBody] CreateTransferRequestDto dto)
        {
            try
            {
                var res = await _transferService.CreateTransferAsync(dto);
                return Ok(res);
            }
            catch (Exception ex)
            {
                return BadRequest($"تعذر إنشاء إذن التحويل: {ex.InnerException?.Message ?? ex.Message}");
            }
        }

        // ========================================================
        // 4. المحطة 2: تدقيق ميزان التكاليف
        // ========================================================
        [HttpPost("audit-costing")]
        public async Task<IActionResult> AuditCosting([FromBody] CostingAuditDto dto)
        {
            try
            {
                var res = await _transferService.AuditCostingAsync(dto);
                return Ok(res);
            }
            catch (Exception ex)
            {
                return BadRequest($"خطأ أثناء تدقيق ميزان التكاليف: {ex.InnerException?.Message ?? ex.Message}");
            }
        }

        // ========================================================
        // 5. فض نزاع ميزان التكاليف والإنتاج
        // ========================================================
        [HttpPost("resolve-costing-dispute")]
        public async Task<IActionResult> ResolveCostingDispute([FromBody] ResolveCostingDisputeDto dto)
        {
            try
            {
                var res = await _transferService.ResolveCostingDisputeAsync(dto);
                return Ok(res);
            }
            catch (Exception ex)
            {
                return BadRequest($"خطأ أثناء فض نزاع التكاليف: {ex.InnerException?.Message ?? ex.Message}");
            }
        }

        // ========================================================
        // 6. المحطة 3: فحص وإفراج الجودة (GMP Release)
        // ========================================================
        [HttpPost("inspect-qc")]
        public async Task<IActionResult> InspectQc([FromBody] QcInspectionDto dto)
        {
            try
            {
                var res = await _transferService.InspectQcAsync(dto);
                return Ok(res);
            }
            catch (Exception ex)
            {
                return BadRequest($"خطأ أثناء فحص الجودة: {ex.InnerException?.Message ?? ex.Message}");
            }
        }

        // ========================================================
        // 7. المحطة 4: إعادة إرسال المرتجع بعد التعديل بالصالة
        // ========================================================
        [HttpPost("resubmit-rework")]
        public async Task<IActionResult> ReSubmitRework([FromBody] ReSubmitReworkDto dto)
        {
            try
            {
                var res = await _transferService.ReSubmitReworkAsync(dto.TransferId, dto.AdjustedQty, dto.AdjustedCount, dto.Notes);
                return Ok(res);
            }
            catch (Exception ex)
            {
                return BadRequest($"خطأ أثناء إعادة إرسال المرتجع المعدل: {ex.InnerException?.Message ?? ex.Message}");
            }
        }

        // ========================================================
        // 8. المحطة 5: استلام وترصيد مخزن الكنترول
        // ========================================================
        [HttpPost("receive-control")]
        public async Task<IActionResult> ReceiveControl([FromBody] ControlReceiptDto dto)
        {
            try
            {
                var res = await _transferService.ReceiveAtControlAsync(dto);
                return Ok(res);
            }
            catch (Exception ex)
            {
                return BadRequest($"خطأ أثناء استلام مخزن الكنترول: {ex.InnerException?.Message ?? ex.Message}");
            }
        }

        // ========================================================
        // 9. فض نزاع الجودة والكنترول واعتماد التسوية
        // ========================================================
        [HttpPost("resolve-dispute")]
        public async Task<IActionResult> ResolveDispute([FromBody] ResolveDisputeDto dto)
        {
            try
            {
                var res = await _transferService.ResolveDisputeAsync(dto);
                return Ok(res);
            }
            catch (Exception ex)
            {
                return BadRequest($"خطأ أثناء فض نزاع الكنترول: {ex.InnerException?.Message ?? ex.Message}");
            }
        }
    }
}