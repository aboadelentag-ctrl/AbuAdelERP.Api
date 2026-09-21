using Microsoft.AspNetCore.Mvc;
using AbuAdelERP.Api.Services;
using AbuAdelERP.Api.DTOs;
using System.Threading.Tasks;
using System;

namespace AbuAdelERP.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class StockController : ControllerBase
{
    private readonly IStockService _stockService;

    public StockController(IStockService stockService)
    {
        _stockService = stockService;
    }

    [HttpPost("add-initial")]
    public async Task<IActionResult> AddInitialStock([FromBody] InitialStockDto dto)
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

    // مسار استدعاء الأرصدة
    [HttpGet("balances")]
    public async Task<IActionResult> GetBalances([FromQuery] long? warehouseId)
    {
        var result = await _stockService.GetStockBalancesAsync(warehouseId);
        return Ok(result);
    }

    // مسار استدعاء كارت الصنف
    [HttpGet("ledger/{itemId}")]
    public async Task<IActionResult> GetItemLedger(long itemId)
    {
        var result = await _stockService.GetItemLedgerAsync(itemId);
        return Ok(result);
    }
}