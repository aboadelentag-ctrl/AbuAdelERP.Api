using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using KayanERP.Api.Models;

namespace KayanERP.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class BranchesController : ControllerBase
{
    private readonly AppDbContext _context;

    public BranchesController(AppDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// جلب قائمة الفروع ومنافذ التشغيل النشطة لبوابة العمليات المركزية.
    /// يتم فلترتها تلقائيًا بواسطة Global Query Filter (ITenantEntity).
    /// </summary>
    [HttpGet("operations-summary")]
    public async Task<ActionResult<IEnumerable<BranchOperationsSummaryDto>>> GetOperationsSummary()
    {
        var branches = await _context.Branches
            .AsNoTracking()
            .Where(b => b.Active)
            .Select(b => new BranchOperationsSummaryDto
            {
                Id = b.Id,
                Code = b.BranchCode,
                NameAr = b.BranchName,
                BranchType = b.Warehouses.Any(w => w.WarehouseType == "RawMaterials")
                    ? "Factory"
                    : "Retail",
                ActiveWarehousesCount = b.Warehouses.Count(w => w.Active),
                TodaySalesVolume = 0.00m,
                IsActive = b.Active
            })
            .OrderBy(b => b.Code)
            .ToListAsync();

        return Ok(branches);
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