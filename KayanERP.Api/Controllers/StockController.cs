using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using KayanERP.Shared.DTOs;
using KayanERP.Api.Services;

namespace KayanERP.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class StockController : ControllerBase
    {
        private readonly IStockService _stockService;

        public StockController(IStockService stockService)
        {
            _stockService = stockService;
        }

        // ==============================================================
        // 1. إضافة رصيد افتتاحي
        // ==============================================================
        [HttpPost("add-initial")]
        public async Task<ActionResult<StockOperationResultDto>> AddInitialStock([FromBody] InitialStockDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var result = await _stockService.AddInitialStockAsync(dto);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"حدث خطأ أثناء إضافة الرصيد: {ex.Message}");
            }
        }

        // ==============================================================
        // 2. استعراض الأرصدة
        // ==============================================================
        [HttpGet("balances")]
        public async Task<ActionResult<List<StockBalanceViewDto>>> GetBalances([FromQuery] long? warehouseId)
        {
            try
            {
                var result = await _stockService.GetStockBalancesAsync(warehouseId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"حدث خطأ أثناء استخراج الأرصدة: {ex.Message}");
            }
        }

        // ==============================================================
        // 3. استعراض حركات الصنف
        // ==============================================================
        [HttpGet("ledger/{itemId}")]
        public async Task<ActionResult<List<StockMovementItemLedgerDto>>> GetItemLedger(long itemId)
        {
            try
            {
                var result = await _stockService.GetItemLedgerAsync(itemId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"حدث خطأ أثناء استخراج سجل حركات الصنف: {ex.Message}");
            }
        }
    }
}