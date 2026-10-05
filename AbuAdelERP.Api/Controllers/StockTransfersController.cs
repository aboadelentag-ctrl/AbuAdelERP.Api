using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AbuAdelERP.Api.DTOs;
using AbuAdelERP.Api.Services;
using AbuAdelERP.Api.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AbuAdelERP.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class StockTransfersController : ControllerBase
    {
        private readonly IStockTransferService _service;
        private readonly AppDbContext _context;

        public StockTransfersController(IStockTransferService service, AppDbContext context)
        {
            _service = service;
            _context = context;
        }

        // ========================================================
        // 1. القوائم المرجعية الموحدة (تنقية المخازن وحصر الأصناف النشطة)
        // ========================================================
        [HttpGet("lookups")]
        public async Task<IActionResult> GetLookups()
        {
            try
            {
                // استبعاد الإدارات والمكاتب المكتبية وحصر القائمة في المخازن ومستودعات الصالات الفعلية
                var warehouses = await _context.Warehouses
                    .AsNoTracking()
                    .Where(w => !w.WarehouseName.Contains("إدارة")
                             && !w.WarehouseName.Contains("العليا")
                             && !w.WarehouseName.Contains("المالية")
                             && !w.WarehouseName.Contains("الموارد")
                             && !w.WarehouseName.Contains("المشتريات")
                             && !w.WarehouseName.Contains("المبيعات"))
                    .OrderBy(w => w.WarehouseName)
                    .Select(w => new { w.Id, w.WarehouseName })
                    .ToListAsync();

                var items = await _context.Items
                    .AsNoTracking()
                    .Where(i => i.Active == true)
                    .OrderBy(i => i.ItemName)
                    .Select(i => new { i.Id, i.ItemCode, i.ItemName, i.Unit, i.ItemType })
                    .ToListAsync();

                return Ok(new { warehouses, items });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"خطأ في جلب البيانات المرجعية للتحويلات: {ex.Message}");
            }
        }

        // ========================================================
        // 2. إدارة وتتبع التحويلات وفض المنازعات
        // ========================================================
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

        [HttpGet("{id:long}")]
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
                return StatusCode(500, $"خطأ في جلب تفاصيل إذن التحويل: {ex.InnerException?.Message ?? ex.Message}");
            }
        }

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

        [HttpPost]
        public async Task<ActionResult<StockTransferDetailDto>> CreateAndDispatch([FromBody] CreateStockTransferDto dto)
        {
            try
            {
                if (dto == null || dto.Lines == null || !dto.Lines.Any())
                    return BadRequest("يجب إدراج صنف واحد على الأقل في إذن التحويل.");

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