using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AbuAdelERP.Api.Models;

namespace AbuAdelERP.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class BranchesController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ILogger<BranchesController> _logger;

    public BranchesController(AppDbContext context, ILogger<BranchesController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// جلب قائمة الفروع ومنافذ التشغيل النشطة لبوابة العمليات المركزية
    /// </summary>
    [HttpGet("operations-summary")]
    public async Task<ActionResult<IEnumerable<BranchOperationsSummaryDto>>> GetOperationsSummary()
    {
        try
        {
            var branches = await _context.Branches
                .AsNoTracking()
                .Where(b => b.Active)
                .Select(b => new BranchOperationsSummaryDto
                {
                    Id = b.Id,
                    Code = b.BranchCode,
                    NameAr = b.BranchName,
                    BranchType = b.Warehouses.Any(w => w.WarehouseType == "RawMaterials") ? "Factory" : "Retail",
                    ActiveWarehousesCount = b.Warehouses.Count(w => w.Active),
                    TodaySalesVolume = 0.00m,
                    IsActive = b.Active
                })
                .OrderBy(b => b.Code)
                .ToListAsync();

            return Ok(branches);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "فشل أثناء جلب بيانات الفروع من قاعدة بيانات Neon");
            return StatusCode(500, new { message = "حدث خطأ أثناء الاتصال بقاعدة البيانات", detail = ex.Message });
        }
    }
}

public sealed class BranchOperationsSummaryDto
{
    public long Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string NameAr { get; set; } = string.Empty;
    public string BranchType { get; set; } = "Retail";
    public int ActiveWarehousesCount { get; set; }
    public decimal TodaySalesVolume { get; set; }
    public bool IsActive { get; set; }
}