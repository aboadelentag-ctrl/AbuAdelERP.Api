using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using KayanERP.Shared.DTOs;
using KayanERP.Api.Models;
using KayanERP.Api.Services;

namespace KayanERP.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class DepartmentsController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ITenantContext _tenantContext;

        public DepartmentsController(AppDbContext context, ITenantContext tenantContext)
        {
            _context = context;
            _tenantContext = tenantContext;
        }

        // ==============================================================
        // 1. جلب كافة الأقسام
        // ==============================================================
        [HttpGet]
        public async Task<ActionResult<IEnumerable<DepartmentDto>>> GetDepartments()
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

        // ==============================================================
        // 2. جلب قسم محدد
        // ==============================================================
        [HttpGet("{id:long}")]
        public async Task<ActionResult<DepartmentDto>> GetDepartment(long id)
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

            if (department == null)
                return NotFound(new { message = "القسم غير موجود." });

            return Ok(department);
        }

        // ==============================================================
        // 3. إضافة قسم جديد
        // ==============================================================
        [HttpPost]
        public async Task<ActionResult<DepartmentDto>> PostDepartment([FromBody] DepartmentDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            dto.DepartmentCode = dto.DepartmentCode?.Trim() ?? string.Empty;
            dto.DepartmentName = dto.DepartmentName?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(dto.DepartmentCode) ||
                string.IsNullOrWhiteSpace(dto.DepartmentName))
                return BadRequest(new { message = "كود واسم القسم مطلوبان." });

            // ⚠️ فرض CompanyId من الـ JWT
            var companyId = _tenantContext.CompanyId;

            bool codeExists = await _context.Departments
                .AnyAsync(d => d.CompanyId == companyId &&
                               d.DepartmentCode.ToLower() == dto.DepartmentCode.ToLower());

            if (codeExists)
                return Conflict(new { message = $"كود القسم ({dto.DepartmentCode}) مسجل بالفعل." });

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
            await _context.SaveChangesAsync();

            dto.Id = department.Id;
            dto.CompanyId = department.CompanyId;

            return CreatedAtAction(nameof(GetDepartment), new { id = department.Id }, dto);
        }

        // ==============================================================
        // 4. تعديل قسم
        // ==============================================================
        [HttpPut("{id:long}")]
        public async Task<IActionResult> PutDepartment(long id, [FromBody] DepartmentDto dto)
        {
            if (dto == null || id != dto.Id)
                return BadRequest(new { message = "بيانات القسم غير متطابقة." });

            if (dto.ParentDepartmentId == id)
                return BadRequest(new { message = "لا يمكن ربط الإدارة بنفسها كجهة تابعة." });

            var existingDepartment = await _context.Departments.FindAsync(id);
            if (existingDepartment == null)
                return NotFound(new { message = "القسم غير موجود." });

            // ⚠️ تأكيد إن القسم يخص الشركة الحالية
            if (existingDepartment.CompanyId != _tenantContext.CompanyId)
                return Forbid();

            dto.DepartmentCode = dto.DepartmentCode?.Trim() ?? string.Empty;
            dto.DepartmentName = dto.DepartmentName?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(dto.DepartmentCode) ||
                string.IsNullOrWhiteSpace(dto.DepartmentName))
                return BadRequest(new { message = "كود واسم القسم مطلوبان." });

            bool codeExists = await _context.Departments
                .AnyAsync(d => d.CompanyId == existingDepartment.CompanyId &&
                               d.DepartmentCode.ToLower() == dto.DepartmentCode.ToLower() &&
                               d.Id != id);

            if (codeExists)
                return Conflict(new { message = $"كود القسم ({dto.DepartmentCode}) مستخدم في قسم آخر." });

            // ⚠️ نحدّث الحقول المسموح بها فقط — نتجاهل CompanyId
            existingDepartment.DepartmentName = dto.DepartmentName;
            existingDepartment.DepartmentCode = dto.DepartmentCode;
            existingDepartment.Description = dto.Description?.Trim() ?? string.Empty;
            existingDepartment.Active = dto.Active;
            existingDepartment.ParentDepartmentId = dto.ParentDepartmentId > 0 ? dto.ParentDepartmentId : null;
            existingDepartment.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return NoContent();
        }

        // ==============================================================
        // 5. حذف قسم (Soft Delete دايمًا)
        // ==============================================================
        [HttpDelete("{id:long}")]
        public async Task<IActionResult> DeleteDepartment(long id)
        {
            var department = await _context.Departments.FindAsync(id);
            if (department == null)
                return NotFound(new { message = "القسم غير موجود." });

            // ⚠️ تأكيد إن القسم يخص الشركة الحالية
            if (department.CompanyId != _tenantContext.CompanyId)
                return Forbid();

            // فحص الأقسام الفرعية
            bool hasSubDepartments = await _context.Departments
                .AnyAsync(d => d.ParentDepartmentId == id);

            if (hasSubDepartments)
                return BadRequest(new { message = "لا يمكن حذف هذه الإدارة لوجود صالات أو أقسام فرعية تابعة لها." });

            // Soft Delete دايمًا — أأمن من Hard Delete
            department.Active = false;
            department.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "تم تعطيل القسم بنجاح." });
        }
    }
}