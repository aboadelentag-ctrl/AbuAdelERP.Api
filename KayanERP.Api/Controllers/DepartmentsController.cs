using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using KayanERP.Api.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KayanERP.Api.Controllers
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

        // ==============================================================
        // 1. جلب كافة الأقسام والإدارات بالهيكل التنظيمي
        // ==============================================================
        [HttpGet]
        public async Task<IActionResult> GetDepartments()
        {
            try
            {
                var departments = await _context.Departments
                    .AsNoTracking()
                    .OrderBy(d => d.DepartmentCode)
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
            catch (Exception ex)
            {
                return StatusCode(500, $"خطأ أثناء جلب الأقسام: {ex.GetBaseException().Message}");
            }
        }

        // ==============================================================
        // 2. جلب بيانات قسم محدد
        // ==============================================================
        [HttpGet("{id:long}")]
        public async Task<IActionResult> GetDepartment(long id)
        {
            try
            {
                var department = await _context.Departments
                    .AsNoTracking()
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

                if (department == null) return NotFound("القسم غير موجود.");
                return Ok(department);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"خطأ في استعلام القسم: {ex.GetBaseException().Message}");
            }
        }

        // ==============================================================
        // 3. إضافة قسم / صالة جديدة
        // ==============================================================
        [HttpPost]
        public async Task<IActionResult> PostDepartment([FromBody] DepartmentDto dto)
        {
            if (dto == null) return BadRequest("بيانات غير صالحة.");

            dto.DepartmentCode = dto.DepartmentCode?.Trim() ?? string.Empty;
            dto.DepartmentName = dto.DepartmentName?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(dto.DepartmentCode) || string.IsNullOrWhiteSpace(dto.DepartmentName))
                return BadRequest("كود واسم القسم مطلوبان.");

            var company = await _context.Companies.FirstOrDefaultAsync();
            long companyId = dto.CompanyId > 0 ? dto.CompanyId : (company?.Id ?? 1);

            bool codeExists = await _context.Departments
                .AnyAsync(d => d.CompanyId == companyId && d.DepartmentCode.ToLower() == dto.DepartmentCode.ToLower());

            if (codeExists)
            {
                return BadRequest($"كود القسم ({dto.DepartmentCode}) مسجل بالفعل، يرجى استخدام كود مختلف.");
            }

            var department = new Department
            {
                CompanyId = companyId,
                DepartmentCode = dto.DepartmentCode,
                DepartmentName = dto.DepartmentName,
                Description = dto.Description?.Trim() ?? string.Empty,
                Active = dto.Active,
                ParentDepartmentId = dto.ParentDepartmentId > 0 ? dto.ParentDepartmentId : null,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Departments.Add(department);

            try
            {
                await _context.SaveChangesAsync();
                dto.Id = department.Id;
                dto.CompanyId = department.CompanyId;
                return Ok(dto);
            }
            catch (DbUpdateException ex)
            {
                return BadRequest($"تعذر حفظ القسم: {ex.GetBaseException().Message}");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"خطأ داخلي: {ex.GetBaseException().Message}");
            }
        }

        // ==============================================================
        // 4. تعديل بيانات قسم
        // ==============================================================
        [HttpPut("{id:long}")]
        public async Task<IActionResult> PutDepartment(long id, [FromBody] DepartmentDto dto)
        {
            if (dto == null || id != dto.Id) return BadRequest("بيانات القسم غير متطابقة.");
            if (dto.ParentDepartmentId == id) return BadRequest("لا يمكن ربط الإدارة بنفسها كجهة تابعة.");

            var existingDepartment = await _context.Departments.FindAsync(id);
            if (existingDepartment == null) return NotFound("القسم غير موجود.");

            dto.DepartmentCode = dto.DepartmentCode?.Trim() ?? string.Empty;
            dto.DepartmentName = dto.DepartmentName?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(dto.DepartmentCode) || string.IsNullOrWhiteSpace(dto.DepartmentName))
                return BadRequest("كود واسم القسم مطلوبان.");

            bool codeExists = await _context.Departments
                .AnyAsync(d => d.CompanyId == existingDepartment.CompanyId
                            && d.DepartmentCode.ToLower() == dto.DepartmentCode.ToLower()
                            && d.Id != id);

            if (codeExists)
            {
                return BadRequest($"كود القسم ({dto.DepartmentCode}) مستخدم في قسم آخر.");
            }

            existingDepartment.DepartmentName = dto.DepartmentName;
            existingDepartment.DepartmentCode = dto.DepartmentCode;
            existingDepartment.Description = dto.Description?.Trim() ?? string.Empty;
            existingDepartment.Active = dto.Active;
            existingDepartment.ParentDepartmentId = dto.ParentDepartmentId > 0 ? dto.ParentDepartmentId : null;
            existingDepartment.UpdatedAt = DateTime.UtcNow;

            try
            {
                await _context.SaveChangesAsync();
                return Ok(existingDepartment);
            }
            catch (DbUpdateException ex)
            {
                return BadRequest($"تعذر تعديل القسم: {ex.GetBaseException().Message}");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"خطأ أثناء التعديل: {ex.GetBaseException().Message}");
            }
        }

        // ==============================================================
        // 5. حذف قسم (محصن ضد انهيار القيود الخارجية)
        // ==============================================================
        [HttpDelete("{id:long}")]
        public async Task<IActionResult> DeleteDepartment(long id)
        {
            try
            {
                var department = await _context.Departments.FindAsync(id);
                if (department == null) return NotFound("القسم غير موجود.");

                bool hasSubDepartments = await _context.Departments.AnyAsync(d => d.ParentDepartmentId == id);
                if (hasSubDepartments)
                    return BadRequest("لا يمكن حذف هذه الإدارة لوجود صالات أو أقسام فرعية تابعة لها في الهيكل التنظيمي.");

                // فحص القيود المرتبطة بالموظفين والأصناف والماكينات
                bool hasEmployees = await _context.Employees.AnyAsync(e => e.DepartmentId == id);
                bool hasItems = await _context.Items.AnyAsync(i => i.DepartmentId == id);
                bool hasAssets = await _context.FixedAssets.AnyAsync(a => a.Department == department.DepartmentName);

                if (hasEmployees || hasItems || hasAssets)
                {
                    department.Active = false;
                    department.UpdatedAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync();
                    return Ok(new { success = true, message = "تم إيقاف وتعطيل القسم بدلاً من حذفه لوجود موظفين أو أصناف أو ماكينات مرتبطة به." });
                }

                _context.Departments.Remove(department);
                await _context.SaveChangesAsync();
                return Ok(new { success = true, message = "تم حذف القسم بنجاح." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"خطأ أثناء معالجة الحذف: {ex.GetBaseException().Message}");
            }
        }
    }

    public class DepartmentDto
    {
        public long Id { get; set; }
        public long CompanyId { get; set; } = 1;
        public bool Active { get; set; } = true;
        public string DepartmentName { get; set; } = string.Empty;
        public string DepartmentCode { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public long? ParentDepartmentId { get; set; }
    }
}