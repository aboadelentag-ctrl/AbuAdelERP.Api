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
    public class StockCardController : ControllerBase
    {
        private readonly IStockCardService _stockCardService;

        public StockCardController(IStockCardService stockCardService)
        {
            _stockCardService = stockCardService;
        }

        // ==============================================================
        // 1. تقرير جرد المخزون الحالي (حسب المخزن أو شامل المنشأة)
        // ==============================================================
        [HttpGet("inventory")]
        public async Task<ActionResult<List<ItemStockSummaryDto>>> GetInventory([FromQuery] long warehouseId = 0)
        {
            try
            {
                var data = await _stockCardService.GetWarehouseInventoryAsync(warehouseId);
                return Ok(data);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"خطأ أثناء جلب جرد المخزون: {ex.Message}");
            }
        }

        // ==============================================================
        // 2. كارت الصنف التفصيلي بحركاته ورصيده التراكمي اللحظي
        // ==============================================================
        [HttpGet("card/{itemId}")]
        public async Task<ActionResult<ItemStockCardDetailDto>> GetCard(
            long itemId,
            [FromQuery] long warehouseId = 0,
            [FromQuery] DateTime? fromDate = null,
            [FromQuery] DateTime? toDate = null)
        {
            try
            {
                var data = await _stockCardService.GetItemStockCardAsync(itemId, warehouseId, fromDate, toDate);
                if (data == null)
                    return NotFound("الصنف غير موجود أو لا توجد بيانات مخزنية مسجلة له.");

                return Ok(data);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"خطأ أثناء استخراج كارت حركة الصنف: {ex.Message}");
            }
        }
    }
}