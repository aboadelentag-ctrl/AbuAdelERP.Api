using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using AbuAdelERP.Api.Services;
using AbuAdelERP.Api.DTOs;

namespace AbuAdelERP.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SalesController : ControllerBase
{
    private readonly ISalesService _salesService;

    public SalesController(ISalesService salesService)
    {
        _salesService = salesService;
    }

    [HttpPost("create-invoice")]
    public async Task<IActionResult> CreateInvoice([FromBody] SalesInvoiceCreateDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        try
        {
            var result = await _salesService.CreateInvoiceAsync(dto);
            return Ok(result);
        }
        catch (System.Exception ex)
        {
            return BadRequest(new { Error = ex.Message });
        }
    }
}