using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using KayanERP.Api.DTOs;
using KayanERP.Api.Models;
using KayanERP.Api.Services;

namespace KayanERP.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class EmployeesController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ITenantContext _tenantContext;

        public EmployeesController(AppDbContext context, ITenantContext tenantContext)
        {
            _context = context;
            _tenantContext = tenantContext;
        }

       
        // ==============================================================
        // 3. جلب الموظفين
        // ==============================================================
        [HttpGet]
        public async Task<ActionResult<IEnumerable<EmployeeListItemDto>>> GetEmployees()
        {
            var employees = await _context.Employees
                .AsNoTracking()
                .Include(e => e.Department)
                .Include(e => e.JobTitle)
                .OrderBy(e => e.EmployeeCode)
                .Select(e => new EmployeeListItemDto
                {
                    Id = e.Id,
                    Name = e.Name,
                    EmployeeCode = e.EmployeeCode,
                    JobTitleId = e.JobTitleId,
                    JobTitle = e.JobTitle != null ? e.JobTitle.TitleName : null,
                    DepartmentId = e.DepartmentId,
                    DepartmentName = e.Department != null ? e.Department.DepartmentName : null,
                    Active = e.Active
                })
                .ToListAsync();

            return Ok(employees);
        }

        // ==============================================================
        // 4. إضافة موظف جديد
        // ==============================================================
        [HttpPost]
        public async Task<ActionResult> CreateEmployee([FromBody] CreateEmployeeDto dto)
        {
            if (!ModelState.IsValid || string.IsNullOrWhiteSpace(dto.Name))
                return BadRequest(new { message = "اسم العامل مطلوب." });

            var companyId = _tenantContext.CompanyId;

            // توليد كود فريد
            string code = !string.IsNullOrWhiteSpace(dto.EmployeeCode)
                ? dto.EmployeeCode.Trim()
                : $"EMP-{Guid.NewGuid().ToString("N")[..8].ToUpper()}";

            // التحقق من عدم التكرار داخل نفس الشركة
            var codeExists = await _context.Employees
                .AnyAsync(e => e.CompanyId == companyId &&
                               e.EmployeeCode.ToLower() == code.ToLower());

            if (codeExists)
                return Conflict(new { message = $"كود الموظف ({code}) مستخدم بالفعل." });

            // التحقق من القسم (داخل نفس الشركة)
            long? deptId = dto.DepartmentId;
            if (deptId.HasValue && deptId > 0)
            {
                var deptExists = await _context.Departments
                    .AnyAsync(d => d.Id == deptId && d.CompanyId == companyId);
                if (!deptExists)
                    return BadRequest(new { message = "القسم المحدد غير موجود." });
            }

            // التحقق من المسمى الوظيفي
            long? jobTitleId = dto.JobTitleId;
            if (jobTitleId.HasValue && jobTitleId > 0)
            {
                var titleExists = await _context.JobTitles
                    .AnyAsync(j => j.Id == jobTitleId && j.CompanyId == companyId);
                if (!titleExists)
                    return BadRequest(new { message = "المسمى الوظيفي غير موجود." });
            }

            var employee = new Employee
            {
                CompanyId = companyId,
                Name = dto.Name.Trim(),
                EmployeeCode = code,
                DepartmentId = deptId,
                JobTitleId = jobTitleId,
                ManagerId = dto.ManagerId > 0 ? dto.ManagerId : null,
                CreatedAt = DateTime.UtcNow,
                Active = dto.Active
            };

            _context.Employees.Add(employee);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetEmployees), new
            {
                employeeId = employee.Id,
                employeeCode = employee.EmployeeCode
            });
        }

        // ==============================================================
        // 5. تعديل بيانات موظف
        // ==============================================================
        [HttpPut("{id:long}")]
        public async Task<IActionResult> UpdateEmployee(long id, [FromBody] UpdateEmployeeDto dto)
        {
            var employee = await _context.Employees.FindAsync(id);
            if (employee == null)
                return NotFound(new { message = "الموظف غير موجود." });

            // ⚠️ تأكيد إن الموظف يخص الشركة الحالية
            if (employee.CompanyId != _tenantContext.CompanyId)
                return Forbid();

            // ⚠️ نحدّث الحقول المسموح بها فقط — نتجاهل CompanyId
            if (!string.IsNullOrWhiteSpace(dto.Name))
                employee.Name = dto.Name.Trim();

            if (!string.IsNullOrWhiteSpace(dto.EmployeeCode))
            {
                var codeExists = await _context.Employees
                    .AnyAsync(e => e.Id != id &&
                                   e.CompanyId == employee.CompanyId &&
                                   e.EmployeeCode.ToLower() == dto.EmployeeCode.Trim().ToLower());
                if (!codeExists)
                    employee.EmployeeCode = dto.EmployeeCode.Trim();
            }

            if (dto.DepartmentId.HasValue && dto.DepartmentId > 0)
            {
                var deptExists = await _context.Departments
                    .AnyAsync(d => d.Id == dto.DepartmentId && d.CompanyId == employee.CompanyId);
                if (deptExists) employee.DepartmentId = dto.DepartmentId;
            }

            if (dto.JobTitleId.HasValue && dto.JobTitleId > 0)
            {
                var titleExists = await _context.JobTitles
                    .AnyAsync(j => j.Id == dto.JobTitleId && j.CompanyId == employee.CompanyId);
                if (titleExists) employee.JobTitleId = dto.JobTitleId;
            }

            employee.Active = dto.Active;

            await _context.SaveChangesAsync();
            return NoContent();
        }

        // ==============================================================
        // 6. تبديل حالة الموظف
        // ==============================================================
        [HttpPut("{id:long}/toggle-status")]
        public async Task<IActionResult> ToggleStatus(long id)
        {
            var employee = await _context.Employees.FindAsync(id);
            if (employee == null)
                return NotFound(new { message = "الموظف غير موجود." });

            if (employee.CompanyId != _tenantContext.CompanyId)
                return Forbid();

            employee.Active = !employee.Active;
            await _context.SaveChangesAsync();

            return Ok(new { active = employee.Active, message = "تم تغيير حالة العامل بنجاح." });
        }

        // ==============================================================
        // 7. حذف موظف (Soft Delete دايمًا)
        // ==============================================================
        [HttpDelete("{id:long}")]
        public async Task<IActionResult> DeleteEmployee(long id)
        {
            var employee = await _context.Employees.FindAsync(id);
            if (employee == null)
                return NotFound(new { message = "العامل غير موجود." });

            if (employee.CompanyId != _tenantContext.CompanyId)
                return Forbid();

            // ⚠️ Soft Delete — أأمن من Hard Delete (بيحافظ على الـ Relationships)
            employee.Active = false;
            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "تم تعطيل العامل بنجاح." });
        }
    }
}