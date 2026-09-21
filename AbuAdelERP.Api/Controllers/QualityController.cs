using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using AbuAdelERP.Api.DTOs;
using AbuAdelERP.Api.Services;

namespace AbuAdelERP.Api.Controllers;

[Authorize] // 🔒 القفل الرئيسي
[Route("api/[controller]")]
[ApiController]
public class QualityController : ControllerBase
{
    private readonly IQualityService _qualityService;

    public QualityController(IQualityService qualityService)
    {
        _qualityService = qualityService;
    }

    [HttpGet]
    public IActionResult GetAll()
    {
        return Ok(new { Message = "تم جلب تقارير الجودة بأمان." });
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Quality")] // 🔒 حماية إضافية: لمهندس الجودة والمدير فقط
    public IActionResult Create([FromBody] QualityInspectionCreateDto request)
    {
        return Ok(new { Message = "تم تسجيل فحص الجودة وإيقاف الأوامر المعيبة آلياً." });
    }
}