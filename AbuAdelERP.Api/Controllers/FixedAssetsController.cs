using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AbuAdelERP.Api.Models;
using AbuAdelERP.Api.DTOs;
using System.Security.Claims;

namespace AbuAdelERP.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FixedAssetsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public FixedAssetsController(AppDbContext context)
        {
            _context = context;
        }

        // 1. استعراض الأصول والماكينات مع الفلاتر
        [HttpGet]
        public async Task<IActionResult> GetAll(
            [FromQuery] string? search,
            [FromQuery] string? department,
            [FromQuery] string? status)
        {
            var query = _context.FixedAssets
                .Include(a => a.HistoryLogs)
                .AsNoTracking()
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(department))
                query = query.Where(a => a.Department == department);

            if (!string.IsNullOrWhiteSpace(status))
                query = query.Where(a => a.Status == status);

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim();
                query = query.Where(a =>
                    a.AssetCode.Contains(term) ||
                    a.Name.Contains(term) ||
                    a.CustodianName.Contains(term) ||
                    (a.SerialNumber != null && a.SerialNumber.Contains(term)));
            }

            var list = await query.OrderBy(a => a.AssetCode).ToListAsync();
            return Ok(list);
        }

        // 2. استعراض السجل التاريخي الكامل للماكينة (Asset Dossier)
        [HttpGet("{id:long}/history")]
        public async Task<IActionResult> GetHistory(long id)
        {
            var asset = await _context.FixedAssets
                .Include(a => a.HistoryLogs)
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.Id == id);

            if (asset == null) return NotFound(new { message = "الأصل غير موجود" });

            return Ok(new
            {
                asset.Id,
                asset.AssetCode,
                asset.Name,
                asset.Department,
                asset.Status,
                Logs = asset.HistoryLogs.OrderByDescending(l => l.Date).ToList()
            });
        }

        // 3. قيد أصل ثابت وماكينة جديدة
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateFixedAssetDto dto)
        {
            if (await _context.FixedAssets.AnyAsync(a => a.AssetCode == dto.AssetCode))
                return BadRequest(new { message = "كود الأصل مسجل مسبقاً لمعدة أخرى!" });

            var empCode = User.FindFirstValue("EmployeeCode") ?? "EMP";
            var empName = User.FindFirstValue(ClaimTypes.Name) ?? "مسؤول النظام";

            var asset = new FixedAsset
            {
                AssetCode = dto.AssetCode,
                Name = dto.Name,
                Department = dto.Department,
                Category = dto.Category,
                BrandModel = dto.BrandModel,
                SerialNumber = dto.SerialNumber,
                CustodianCode = dto.CustodianCode,
                CustodianName = dto.CustodianName,
                InstallationDate = dto.InstallationDate,
                Status = "OPERATIONAL"
            };

            asset.HistoryLogs.Add(new AssetHistoryLog
            {
                Date = DateTime.UtcNow,
                EventType = "شراء وتركيب",
                Description = $"قيد الأصل وتثبيته في صالة ({dto.Department}) بعهدة ({dto.CustodianName})",
                PerformedByCode = empCode,
                PerformedByName = empName,
                CostImpact = "استثماري"
            });

            _context.FixedAssets.Add(asset);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                id = asset.Id,
                assetCode = asset.AssetCode,
                name = asset.Name,
                message = "تم تحديث بيانات الماكينة بنجاح"
            });
        }

        // 4. تعديل الأصل ونقل العهدة
        [HttpPut("{id:long}")]
        public async Task<IActionResult> Update(long id, [FromBody] UpdateFixedAssetDto dto)
        {
            var asset = await _context.FixedAssets
                .Include(a => a.HistoryLogs)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (asset == null) return NotFound();

            var empCode = User.FindFirstValue("EmployeeCode") ?? "EMP";
            var empName = User.FindFirstValue(ClaimTypes.Name) ?? "المشرف";

            if (asset.Department != dto.Department || asset.CustodianCode != dto.CustodianCode)
            {
                asset.HistoryLogs.Add(new AssetHistoryLog
                {
                    Date = DateTime.UtcNow,
                    EventType = "نقل عهدة / موقع",
                    Description = $"نقل من ({asset.Department} - {asset.CustodianName}) إلى ({dto.Department} - {dto.CustodianName})",
                    PerformedByCode = empCode,
                    PerformedByName = empName,
                    CostImpact = "إداري"
                });
            }

            asset.Name = dto.Name;
            asset.Department = dto.Department;
            asset.Category = dto.Category;
            asset.BrandModel = dto.BrandModel;
            asset.SerialNumber = dto.SerialNumber;
            asset.CustodianCode = dto.CustodianCode;
            asset.CustodianName = dto.CustodianName;
            asset.Status = dto.Status;

            await _context.SaveChangesAsync();
            return Ok(asset);
        }
    }
}