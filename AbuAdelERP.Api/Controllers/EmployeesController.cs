using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AbuAdelERP.Api.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AbuAdelERP.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EmployeesController : ControllerBase
    {
        private readonly AppDbContext _context;

        public EmployeesController(AppDbContext context)
        {
            _context = context;
        }

        // ==============================================================
        // 1. إدارة المسميات الوظيفية (جلب + إضافة سريعة محصنة)
        // ==============================================================
        [HttpGet("job-titles")]
        public async Task<IActionResult> GetJobTitles()
        {
            try
            {
                // استعلام نقي يجلب فقط المعرف والاسم لتجنب أي خطأ في حال عدم وجود عمود hourly_rate
                var titles = await _context.JobTitles
                    .AsNoTracking()
                    .Where(j => j.Active)
                    .Select(j => new JobTitleLookupDto
                    {
                        Id = j.Id,
                        TitleName = j.TitleName
                    })
                    .ToListAsync();

                return Ok(titles);
            }
            catch (Exception ex)
            {
                var inner = ex.InnerException?.Message ?? ex.Message;
                return StatusCode(500, $"خطأ في جلب الوظائف: {inner}");
            }
        }

        [HttpPost("job-titles")]
        public async Task<IActionResult> CreateJobTitle([FromBody] CreateJobTitleDto dto)
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.TitleName))
            {
                return BadRequest("يرجى إدخال اسم المسمى الوظيفي.");
            }

            try
            {
                var trimmedName = dto.TitleName.Trim();

                // التحقق من وجود المسمى مسبقاً لمنع التكرار
                var existing = await _context.JobTitles
                    .FirstOrDefaultAsync(j => j.TitleName.ToLower() == trimmedName.ToLower());

                if (existing != null)
                {
                    return Ok(new
                    {
                        Message = "المسمى الوظيفي متواجد بالفعل!",
                        Id = existing.Id,
                        TitleName = existing.TitleName
                    });
                }

                var company = await _context.Companies.FirstOrDefaultAsync();
                var companyId = company != null ? company.Id : 1;

                var jobTitle = new JobTitle
                {
                    CompanyId = companyId,
                    TitleName = trimmedName,
                    JobGrade = dto.JobGrade > 0 ? dto.JobGrade : 10,
                    Active = true
                };

                _context.JobTitles.Add(jobTitle);
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    Message = "تم إضافة المسمى الوظيفي بنجاح!",
                    Id = jobTitle.Id,
                    TitleName = jobTitle.TitleName
                });
            }
            catch (Exception ex)
            {
                var inner = ex.InnerException?.InnerException?.Message
                         ?? ex.InnerException?.Message
                         ?? ex.Message;
                return StatusCode(500, $"خطأ في حفظ المسمى الوظيفي: {inner}");
            }
        }

        // ==============================================================
        // 2. جلب الموظفين بالكامل
        // ==============================================================
        [HttpGet]
        public async Task<IActionResult> GetEmployeesFlat()
        {
            try
            {
                var employees = await _context.Employees
                    .AsNoTracking()
                    .Include(e => e.Department)
                    .Include(e => e.JobTitle)
                    .Select(e => new
                    {
                        e.Id,
                        e.Name,
                        EmployeeName = e.Name,
                        WorkerCode = e.EmployeeCode,
                        EmployeeCode = e.EmployeeCode,
                        JobTitleId = e.JobTitleId,
                        JobTitle = e.JobTitle != null ? e.JobTitle.TitleName : "عامل تشغيل",
                        JobTitleName = e.JobTitle != null ? e.JobTitle.TitleName : "عامل تشغيل",
                        DepartmentId = e.DepartmentId,
                        DepartmentName = e.Department != null ? e.Department.DepartmentName : "غير محدد",
                        e.Active
                    })
                    .ToListAsync();

                return Ok(employees);
            }
            catch (Exception ex)
            {
                var inner = ex.InnerException?.Message ?? ex.Message;
                return StatusCode(500, $"خطأ في جلب الموظفين: {inner}");
            }
        }

        // ==============================================================
        // 3. إضافة موظف جديد
        // ==============================================================
        [HttpPost]
        public async Task<IActionResult> CreateEmployee([FromBody] CreateEmployeeDto dto)
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.Name) || !dto.DepartmentId.HasValue || dto.DepartmentId <= 0)
            {
                return BadRequest("اسم العامل والقسم مطلوبان.");
            }

            try
            {
                var company = await _context.Companies.FirstOrDefaultAsync();
                var companyId = company != null ? company.Id : 1;
                var code = !string.IsNullOrWhiteSpace(dto.WorkerCode) ? dto.WorkerCode : dto.EmployeeCode;

                var employee = new Employee
                {
                    CompanyId = companyId,
                    Name = dto.Name.Trim(),
                    EmployeeCode = code?.Trim() ?? string.Empty,
                    DepartmentId = dto.DepartmentId > 0 ? dto.DepartmentId : null,
                    JobTitleId = dto.JobTitleId > 0 ? dto.JobTitleId : null,
                    ManagerId = dto.ManagerId > 0 ? dto.ManagerId : null,
                    CreatedAt = DateTime.UtcNow,
                    Active = dto.Active
                };

                _context.Employees.Add(employee);
                await _context.SaveChangesAsync();
                return Ok(new { Message = "تم حفظ العامل بنجاح!", EmployeeId = employee.Id });
            }
            catch (Exception ex)
            {
                var inner = ex.InnerException?.Message ?? ex.Message;
                return StatusCode(500, $"خطأ أثناء حفظ العامل: {inner}");
            }
        }

        // ==============================================================
        // 4. تعديل بيانات موظف / نقله إلى قسم آخر / تغيير وظيفته
        // ==============================================================
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateEmployee(long id, [FromBody] UpdateEmployeeDto dto)
        {
            try
            {
                var employee = await _context.Employees.FindAsync(id);
                if (employee == null)
                {
                    return NotFound("الموظف غير موجود.");
                }

                var code = !string.IsNullOrWhiteSpace(dto.WorkerCode) ? dto.WorkerCode : dto.EmployeeCode;

                employee.Name = dto.Name.Trim();
                employee.EmployeeCode = code?.Trim() ?? string.Empty;
                employee.DepartmentId = dto.DepartmentId > 0 ? dto.DepartmentId : null;
                employee.JobTitleId = dto.JobTitleId > 0 ? dto.JobTitleId : null;
                employee.Active = dto.Active;

                await _context.SaveChangesAsync();
                return Ok(new { Message = "تم تحديث بيانات العامل بنجاح!" });
            }
            catch (Exception ex)
            {
                var inner = ex.InnerException?.Message ?? ex.Message;
                return StatusCode(500, $"خطأ أثناء تحديث بيانات العامل: {inner}");
            }
        }

        // ==============================================================
        // 5. تبديل حالة العامل (نشط / موقوف) بنقرة زر
        // ==============================================================
        [HttpPut("{id}/toggle-status")]
        public async Task<IActionResult> ToggleStatus(long id)
        {
            try
            {
                var employee = await _context.Employees.FindAsync(id);
                if (employee == null)
                {
                    return NotFound("الموظف غير موجود.");
                }

                employee.Active = !employee.Active;
                await _context.SaveChangesAsync();
                return Ok(new { Message = "تم تغيير حالة العامل بنجاح!", Active = employee.Active });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"خطأ: {ex.Message}");
            }
        }

        // ==============================================================
        // 6. حذف العامل نهائياً من قاعدة البيانات
        // ==============================================================
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteEmployee(long id)
        {
            try
            {
                var employee = await _context.Employees.FindAsync(id);
                if (employee == null)
                {
                    return NotFound("العامل غير موجود.");
                }

                _context.Employees.Remove(employee);
                await _context.SaveChangesAsync();
                return Ok(new { Message = "تم حذف العامل نهائياً بنجاح!" });
            }
            catch (Exception ex)
            {
                var inner = ex.InnerException?.Message ?? ex.Message;
                return StatusCode(500, $"تعذر حذف العامل: {inner}");
            }
        }
    }

    public class JobTitleLookupDto
    {
        public long Id { get; set; }
        public string TitleName { get; set; } = string.Empty;
        public decimal HourlyRate { get; set; } = 25;
    }

    public class CreateJobTitleDto
    {
        public string TitleName { get; set; } = string.Empty;
        public int JobGrade { get; set; } = 10;
        public decimal HourlyRate { get; set; } = 25;
    }

    public class CreateEmployeeDto
    {
        public string Name { get; set; } = string.Empty;
        public string WorkerCode { get; set; } = string.Empty;
        public string EmployeeCode { get; set; } = string.Empty;
        public long? DepartmentId { get; set; }
        public long? JobTitleId { get; set; }
        public long? ManagerId { get; set; }
        public bool Active { get; set; } = true;
    }

    public class UpdateEmployeeDto
    {
        public string Name { get; set; } = string.Empty;
        public string WorkerCode { get; set; } = string.Empty;
        public string EmployeeCode { get; set; } = string.Empty;
        public long? DepartmentId { get; set; }
        public long? JobTitleId { get; set; }
        public bool Active { get; set; } = true;
    }
}