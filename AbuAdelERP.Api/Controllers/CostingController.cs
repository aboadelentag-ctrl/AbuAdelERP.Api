using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using AbuAdelERP.Api.Services;
using AbuAdelERP.Api.DTOs;

namespace AbuAdelERP.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CostingController : ControllerBase
{
    private readonly ICostingService _costingService;

    public CostingController(ICostingService costingService)
    {
        _costingService = costingService;
    }

    [HttpPost("calculate-batch")]
    public async Task<IActionResult> CalculateBatch([FromBody] BatchCostRequestDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        try
        {
            var result = await _costingService.CalculateBatchCostAsync(dto);
            return Ok(result);
        }
        catch (System.Exception ex)
        {
            return BadRequest(new { Error = ex.Message });
        }
    }
}