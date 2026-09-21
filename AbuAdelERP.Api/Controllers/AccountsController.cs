using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using AbuAdelERP.Api.Services;
using AbuAdelERP.Api.DTOs;
using System;

namespace AbuAdelERP.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AccountsController : ControllerBase
{
    private readonly IAccountService _accountService;

    public AccountsController(IAccountService accountService)
    {
        _accountService = accountService;
    }

    [HttpPost("create")]
    public async Task<IActionResult> CreateAccount([FromBody] AccountCreateDto dto)
    {
        try
        {
            var result = await _accountService.CreateAccountAsync(dto);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { Error = ex.Message });
        }
    }

    [HttpGet("get-all")]
    public async Task<IActionResult> GetAll()
    {
        var result = await _accountService.GetAllAccountsAsync();
        return Ok(result);
    }
}