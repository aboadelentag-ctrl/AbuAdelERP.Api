using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using KayanERP.Shared.DTOs;
using KayanERP.Api.Services;

namespace KayanERP.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
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
            var balance = await _crateService.GetFactoryBalanceAsync();
            return Ok(balance);
        }

        // ========================================================
        // 2. كشف حركات عهدة العميل
        // ========================================================
        [HttpGet("client/{clientId:long}")]
        public async Task<ActionResult<List<CrateClientTransactionViewDto>>> GetClientTransactions(long clientId)
        {
            var transactions = await _crateService.GetClientTransactionsAsync(clientId);
            return Ok(transactions);
        }

        // ========================================================
        // 3. قيد حركة عهدة مع العميل (DISPATCH / RETURN)
        // ========================================================
        [HttpPost]
        public async Task<ActionResult<CrateClientTransactionViewDto>> PostTransaction(
            [FromBody] CreateCrateClientTransactionDto dto)
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
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}