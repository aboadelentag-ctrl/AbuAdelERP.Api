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
    public class CrateClientTransactionsController : ControllerBase
    {
        private readonly ICrateService _crateService;

        public CrateClientTransactionsController(ICrateService crateService)
        {
            _crateService = crateService;
        }

        // ========================================================
        // 1. رصيد المصنع الفعلي من الصناديق
        // ========================================================
        [HttpGet("factory-balance")]
        public async Task<ActionResult<int>> GetFactoryBalance()
        {
            try
            {
                var balance = await _crateService.GetFactoryBalanceAsync();
                return Ok(balance);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"خطأ أثناء جلب رصيد المصنع: {ex.Message}");
            }
        }

        // ========================================================
        // 2. كشف حركات عهدة العميل
        // ========================================================
        [HttpGet("client/{clientId:int}")]
        public async Task<ActionResult<List<CrateClientTransactionViewDto>>> GetClientTransactions(int clientId)
        {
            try
            {
                var transactions = await _crateService.GetClientTransactionsAsync(clientId);
                return Ok(transactions);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"خطأ أثناء جلب حركات العميل: {ex.Message}");
            }
        }

        // ========================================================
        // 3. قيد حركة عهدة مع العميل (منصرف / مستلم)
        // ========================================================
        [HttpPost]
        public async Task<ActionResult<CrateClientTransactionViewDto>> PostTransaction([FromBody] CreateCrateClientTransactionDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var result = await _crateService.AddClientTransactionAsync(dto);
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"خطأ أثناء تسجيل حركة العهدة: {ex.Message}");
            }
        }
    }
}