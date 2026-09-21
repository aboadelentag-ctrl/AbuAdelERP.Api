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
    public class StockCardController : ControllerBase
    {
        private readonly IStockCardService _stockCardService;

        public StockCardController(IStockCardService stockCardService)
        {
            _stockCardService = stockCardService;
        }

        [HttpGet("inventory")]
        public async Task<ActionResult<List<ItemStockSummaryDto>>> GetInventory([FromQuery] long warehouseId = 0)
        {
            var data = await _stockCardService.GetWarehouseInventoryAsync(warehouseId);
            return Ok(data);
        }

        [HttpGet("card/{itemId}")]
        public async Task<ActionResult<ItemStockCardDetailDto>> GetCard(
            long itemId,
            [FromQuery] long warehouseId = 0,
            [FromQuery] DateTime? fromDate = null,
            [FromQuery] DateTime? toDate = null)
        {
            var data = await _stockCardService.GetItemStockCardAsync(itemId, warehouseId, fromDate, toDate);
            if (data == null) return NotFound("الصنف غير موجود.");
            return Ok(data);
        }
    }
}