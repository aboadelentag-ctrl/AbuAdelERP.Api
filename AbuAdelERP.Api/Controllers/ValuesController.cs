using Microsoft.AspNetCore.Mvc;

namespace AbuAdelERP.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class ValuesController : ControllerBase
{
    // دالة بسيطة فقط للتأكد من حالة السيرفر
    [HttpGet("status")]
    public IActionResult GetStatus()
    {
        return Ok(new { Message = "ERP API is running successfully!" });
    }
}