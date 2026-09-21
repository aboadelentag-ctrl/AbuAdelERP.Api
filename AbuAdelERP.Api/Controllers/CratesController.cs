using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using AbuAdelERP.Api.DTOs;
using AbuAdelERP.Api.Services;

namespace AbuAdelERP.Api.Controllers;

[Authorize] // 🔒 القفل الرئيسي: يمنع الدخول بدون تصريح
[Route("api/[controller]")]
[ApiController]
public class CratesController : ControllerBase
{
    private readonly ICrateService _crateService;

    public CratesController(ICrateService crateService)
    {
        _crateService = crateService;
    }

    [HttpGet]
    public IActionResult GetAll()
    {
        return Ok(new { Message = "تم جلب بيانات الاسبات بأمان." });
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Sales")] // 🔒 حماية إضافية: للمدير والمبيعات فقط
    public IActionResult Create([FromBody] CrateTransactionCreateDto request)
    {
        // سيتم ربطها لاحقاً بالخدمة الفعلية
        return Ok(new { Message = "تم تسجيل حركة الاسبات بنجاح." });
    }
}