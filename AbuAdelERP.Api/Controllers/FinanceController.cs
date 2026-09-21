using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using AbuAdelERP.Api.Services;
using System;

namespace AbuAdelERP.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class FinanceController : ControllerBase
{
    private readonly IFinanceService _financeService;

    public FinanceController(IFinanceService financeService)
    {
        _financeService = financeService;
    }

    [HttpGet("journal-entries")]
    public async Task<IActionResult> GetJournalEntries()
    {
        try
        {
            var result = await _financeService.GetLatestJournalEntriesAsync();
            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"حدث خطأ أثناء جلب القيود: {ex.Message}");
        }
    }
}