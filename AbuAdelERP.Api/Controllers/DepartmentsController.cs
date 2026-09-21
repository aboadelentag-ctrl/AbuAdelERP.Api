using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AbuAdelERP.Api.Models;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace AbuAdelERP.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DepartmentsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public DepartmentsController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetDepartments()
        {
            var departments = await _context.Departments
                .Select(d => new DepartmentDto
                {
                    Id = d.Id,
                    CompanyId = d.CompanyId,
                    Active = d.Active,
                    DepartmentName = d.DepartmentName,
                    DepartmentCode = d.DepartmentCode,
                    Description = d.Description,
                    ParentDepartmentId = d.ParentDepartmentId
                })
                .ToListAsync();

            return Ok(departments);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetDepartment(long id)
        {
            var department = await _context.Departments
                .Where(d => d.Id == id)
                .Select(d => new DepartmentDto
                {
                    Id = d.Id,
                    CompanyId = d.CompanyId,
                    Active = d.Active,
                    DepartmentName = d.DepartmentName,
                    DepartmentCode = d.DepartmentCode,
                    Description = d.Description,
                    ParentDepartmentId = d.ParentDepartmentId
                })
                .FirstOrDefaultAsync();

            if (department == null) return NotFound();
            return Ok(department);
        }

        [HttpPost]
        public async Task<IActionResult> PostDepartment([FromBody] DepartmentDto dto)
        {
            var company = await _context.Companies.FirstOrDefaultAsync();
            if (company == null) return BadRequest("لا توجد شركات مسجلة في قاعدة البيانات.");

            // تنظيف المدخلات من المسافات الزائدة
            dto.DepartmentCode = dto.DepartmentCode?.Trim() ?? string.Empty;

            bool codeExists = await _context.Departments
                .AnyAsync(d => d.CompanyId == company.Id && d.DepartmentCode.ToLower() == dto.DepartmentCode.ToLower());

            if (codeExists)
            {
                return BadRequest($"كود القسم ({dto.DepartmentCode}) مسجل بالفعل، يرجى استخدام كود مختلف.");
            }

            var department = new Department
            {
                CompanyId = company.Id,
                DepartmentCode = dto.DepartmentCode,
                DepartmentName = dto.DepartmentName?.Trim() ?? string.Empty,
                Description = dto.Description?.Trim() ?? string.Empty,
                Active = dto.Active,
                ParentDepartmentId = dto.ParentDepartmentId,
                CreatedAt = DateTime.UtcNow
            };

            _context.Departments.Add(department);

            try
            {
                await _context.SaveChangesAsync();
                dto.Id = department.Id;
                dto.CompanyId = department.CompanyId;
                return Ok(dto);
            }
            // اصطياد أخطاء تكرار الكود من قاعدة البيانات مباشرة (للحماية من النقرات المزدوجة)
            catch (DbUpdateException ex) when (ex.InnerException is Npgsql.PostgresException pgEx && pgEx.SqlState == "23505")
            {
                return BadRequest($"عفواً، كود القسم ({dto.DepartmentCode}) تم تسجيله مسبقاً. الرجاء استخدام كود آخر.");
            }
            catch (Exception ex)
            {
                var errorMsg = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                return StatusCode(500, $"خطأ داخلي: {errorMsg}");
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> PutDepartment(long id, [FromBody] DepartmentDto dto)
        {
            if (id != dto.Id) return BadRequest("رقم القسم غير متطابق.");
            if (dto.ParentDepartmentId == id) return BadRequest("لا يمكن ربط الإدارة بنفسها كجهة تابعة.");

            var existingDepartment = await _context.Departments.FindAsync(id);
            if (existingDepartment == null) return NotFound("القسم غير موجود.");

            dto.DepartmentCode = dto.DepartmentCode?.Trim() ?? string.Empty;

            bool codeExists = await _context.Departments
                .AnyAsync(d => d.CompanyId == existingDepartment.CompanyId
                            && d.DepartmentCode.ToLower() == dto.DepartmentCode.ToLower()
                            && d.Id != id);

            if (codeExists)
            {
                return BadRequest($"كود القسم ({dto.DepartmentCode}) مستخدم في قسم آخر.");
            }

            existingDepartment.DepartmentName = dto.DepartmentName?.Trim() ?? string.Empty;
            existingDepartment.DepartmentCode = dto.DepartmentCode;
            existingDepartment.Description = dto.Description?.Trim() ?? string.Empty;
            existingDepartment.Active = dto.Active;
            existingDepartment.ParentDepartmentId = dto.ParentDepartmentId;
            existingDepartment.UpdatedAt = DateTime.UtcNow;

            try
            {
                await _context.SaveChangesAsync();
                return NoContent();
            }
            catch (DbUpdateException ex) when (ex.InnerException is Npgsql.PostgresException pgEx && pgEx.SqlState == "23505")
            {
                return BadRequest($"عفواً، كود القسم ({dto.DepartmentCode}) مستخدم بالفعل.");
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteDepartment(long id)
        {
            var department = await _context.Departments.FindAsync(id);
            if (department == null) return NotFound();

            bool hasSubDepartments = await _context.Departments.AnyAsync(d => d.ParentDepartmentId == id);
            if (hasSubDepartments) return BadRequest("لا يمكن حذف هذه الإدارة لارتباط أقسام فرعية بها.");

            _context.Departments.Remove(department);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }

    public class DepartmentDto
    {
        public long Id { get; set; }
        public long CompanyId { get; set; }
        public bool Active { get; set; }
        public string DepartmentName { get; set; } = string.Empty;
        public string DepartmentCode { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public long? ParentDepartmentId { get; set; }
    }
}