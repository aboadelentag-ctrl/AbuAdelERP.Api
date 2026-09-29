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
    public class StockTransfersController : ControllerBase
    {
        private readonly IStockTransferService _service;

        public StockTransfersController(IStockTransferService service)
        {
            _service = service;
        }

        // 1. جلب قائمة التحويلات (مع إمكانية الفلترة بحسب الحالة)
        [HttpGet]
        public async Task<ActionResult<List<StockTransferDetailDto>>> GetAll([FromQuery] string? status)
        {
            try
            {
                var result = await _service.GetAllTransfersAsync(status);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"خطأ في جلب بيانات التحويلات: {ex.InnerException?.Message ?? ex.Message}");
            }
        }

        // 2. جلب تفاصيل إذن تحويل محدد برقم الـ Id
        [HttpGet("{id}")]
        public async Task<ActionResult<StockTransferDetailDto>> GetById(long id)
        {
            try
            {
                var result = await _service.GetTransferByIdAsync(id);
                if (result == null) return NotFound($"إذن التحويل رقم {id} غير موجود.");
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"خطأ في جلب بيانات إذن التحويل: {ex.InnerException?.Message ?? ex.Message}");
            }
        }

        // 3. جلب الرصيد اللحظي واللوطات المتاحة (FEFO) لصنف معين في مخزن محدد
        [HttpGet("available-batches")]
        public async Task<ActionResult<AvailableStockInfoDto>> GetAvailableBatches([FromQuery] long itemId, [FromQuery] long warehouseId)
        {
            try
            {
                if (itemId <= 0 || warehouseId <= 0)
                    return BadRequest("معرّف الصنف والمخزن مطلوبان للاستعلام.");

                var result = await _service.GetAvailableBatchesAsync(itemId, warehouseId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"خطأ في استعلام رصيد ولوطات الصنف: {ex.InnerException?.Message ?? ex.Message}");
            }
        }

        // 4. إنشاء إذن تحويل جديد وصرفه فورياً من المخزن الراسل (In-Transit)
        [HttpPost]
        public async Task<ActionResult<StockTransferDetailDto>> CreateAndDispatch([FromBody] CreateStockTransferDto dto)
        {
            try
            {
                if (dto == null) return BadRequest("بيانات إذن التحويل غير صالحة.");
                var result = await _service.CreateAndDispatchTransferAsync(dto);
                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"خطأ أثناء إنشاء وصرف التحويل: {ex.InnerException?.Message ?? ex.Message}");
            }
        }

        // 5. تأكيد استلام سطر تحويل في المخزن المستقبل صنفاً بصنف
        [HttpPost("receive-line")]
        public async Task<IActionResult> ReceiveLine([FromBody] ConfirmTransferLineReceiveDto dto)
        {
            try
            {
                if (dto == null) return BadRequest("بيانات تأكيد الاستلام غير صالحة.");
                var success = await _service.ReceiveTransferLineAsync(dto);
                if (!success) return BadRequest("تعذر تأكيد استلام سطر التحويل، يرجى التحقق من صحة السطر.");
                return Ok(true);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"خطأ أثناء تسجيل استلام السطر: {ex.InnerException?.Message ?? ex.Message}");
            }
        }

        // 6. اعتماد قرار لجنة فض المنازعات وتسوية الفروق وعجز النقل
        [HttpPost("resolve-dispute")]
        public async Task<IActionResult> ResolveDispute([FromBody] ResolveTransferDisputeDto dto)
        {
            try
            {
                if (dto == null) return BadRequest("بيانات محضر فض النزاع غير صالحة.");
                var success = await _service.ResolveTransferDisputeAsync(dto);
                if (!success) return BadRequest("تعذر اعتماد تسوية النزاع، يرجى التحقق من صحة السطر.");
                return Ok(true);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"خطأ أثناء اعتماد تسوية النزاع: {ex.InnerException?.Message ?? ex.Message}");
            }
        }
    }
}