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
                var titles = await _context.JobTitles
                    .AsNoTracking()
                    .Where(j => j.Active)
                    .OrderBy(j => j.TitleName)
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
                return StatusCode(500, $"خطأ في جلب الوظائف: {ex.GetBaseException().Message}");
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
                return StatusCode(500, $"خطأ في حفظ المسمى الوظيفي: {ex.GetBaseException().Message}");
            }
        }

        // ==============================================================
        // 2. جلب الموظفين بالكامل مع دعم التسميات المتعددة (Flat API)
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
                    .OrderBy(e => e.EmployeeCode)
                    .Select(e => new
                    {
                        e.Id,
                        e.Name,
                        FullName = e.Name,
                        EmployeeName = e.Name,
                        Code = e.EmployeeCode,
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
                return StatusCode(500, $"خطأ في جلب الموظفين: {ex.GetBaseException().Message}");
            }
        }

        // ==============================================================
        // 3. إضافة موظف جديد (محصن ضد نقص المعرفات والأسماء)
        // ==============================================================
        [HttpPost]
        public async Task<IActionResult> CreateEmployee([FromBody] CreateEmployeeDto dto)
        {
            if (dto == null) return BadRequest("بيانات غير صالحة.");

            var resolvedName = !string.IsNullOrWhiteSpace(dto.Name) ? dto.Name :
                               (!string.IsNullOrWhiteSpace(dto.FullName) ? dto.FullName : dto.EmployeeName);

            if (string.IsNullOrWhiteSpace(resolvedName))
            {
                return BadRequest("اسم العامل مطلوب.");
            }

            try
            {
                var company = await _context.Companies.FirstOrDefaultAsync();
                var companyId = company != null ? company.Id : 1;

                var code = !string.IsNullOrWhiteSpace(dto.WorkerCode) ? dto.WorkerCode :
                           (!string.IsNullOrWhiteSpace(dto.EmployeeCode) ? dto.EmployeeCode : dto.Code);

                if (string.IsNullOrWhiteSpace(code))
                {
                    int nextId = (await _context.Employees.MaxAsync(e => (int?)e.Id) ?? 0) + 1;
                    code = $"EMP-{nextId:D3}";
                }
                else
                {
                    code = code.Trim();
                }

                // فحص تكرار الكود مع موظف آخر
                var codeExists = await _context.Employees.AnyAsync(e => e.EmployeeCode.ToLower() == code.ToLower());
                if (codeExists)
                {
                    code = $"{code}-{new Random().Next(10, 99)}";
                }

                // تسوية القسم (بالرقم أو بالاسم)
                long? deptId = dto.DepartmentId;
                if ((!deptId.HasValue || deptId <= 0) && !string.IsNullOrWhiteSpace(dto.DepartmentName))
                {
                    var deptMatch = await _context.Departments.FirstOrDefaultAsync(d =>
                        d.DepartmentName.ToLower() == dto.DepartmentName.Trim().ToLower() ||
                        d.DepartmentCode.ToLower() == dto.DepartmentName.Trim().ToLower());
                    if (deptMatch != null) deptId = deptMatch.Id;
                }

                if (!deptId.HasValue || deptId <= 0)
                {
                    deptId = (await _context.Departments.FirstOrDefaultAsync())?.Id ?? 1;
                }

                // تسوية المسمى الوظيفي
                long? jobTitleId = dto.JobTitleId;
                var jobText = !string.IsNullOrWhiteSpace(dto.JobTitle) ? dto.JobTitle : dto.JobTitleName;
                if ((!jobTitleId.HasValue || jobTitleId <= 0) && !string.IsNullOrWhiteSpace(jobText))
                {
                    var titleMatch = await _context.JobTitles.FirstOrDefaultAsync(j => j.TitleName.ToLower() == jobText.Trim().ToLower());
                    if (titleMatch != null)
                    {
                        jobTitleId = titleMatch.Id;
                    }
                    else
                    {
                        var newTitle = new JobTitle
                        {
                            CompanyId = companyId,
                            TitleName = jobText.Trim(),
                            JobGrade = 10,
                            Active = true
                        };
                        _context.JobTitles.Add(newTitle);
                        await _context.SaveChangesAsync();
                        jobTitleId = newTitle.Id;
                    }
                }

                var employee = new Employee
                {
                    CompanyId = companyId,
                    Name = resolvedName.Trim(),
                    EmployeeCode = code,
                    DepartmentId = deptId,
                    JobTitleId = jobTitleId,
                    ManagerId = dto.ManagerId > 0 ? dto.ManagerId : null,
                    CreatedAt = DateTime.UtcNow,
                    Active = dto.Active
                };

                _context.Employees.Add(employee);
                await _context.SaveChangesAsync();

                return Ok(new { Message = "تم حفظ العامل بنجاح!", EmployeeId = employee.Id, EmployeeCode = employee.EmployeeCode });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"خطأ أثناء حفظ العامل: {ex.GetBaseException().Message}");
            }
        }

        // ==============================================================
        // 4. تعديل بيانات موظف / نقله إلى قسم آخر / تغيير وظيفته
        // ==============================================================
        [HttpPut("{id:long}")]
        public async Task<IActionResult> UpdateEmployee(long id, [FromBody] UpdateEmployeeDto dto)
        {
            if (dto == null) return BadRequest("بيانات غير صالحة.");

            try
            {
                var employee = await _context.Employees.FindAsync(id);
                if (employee == null) return NotFound("الموظف غير موجود.");

                var resolvedName = !string.IsNullOrWhiteSpace(dto.Name) ? dto.Name :
                                   (!string.IsNullOrWhiteSpace(dto.FullName) ? dto.FullName : dto.EmployeeName);

                if (!string.IsNullOrWhiteSpace(resolvedName))
                    employee.Name = resolvedName.Trim();

                var code = !string.IsNullOrWhiteSpace(dto.WorkerCode) ? dto.WorkerCode :
                           (!string.IsNullOrWhiteSpace(dto.EmployeeCode) ? dto.EmployeeCode : dto.Code);

                if (!string.IsNullOrWhiteSpace(code))
                {
                    code = code.Trim();
                    var codeExists = await _context.Employees.AnyAsync(e => e.Id != id && e.EmployeeCode.ToLower() == code.ToLower());
                    if (!codeExists)
                    {
                        employee.EmployeeCode = code;
                    }
                }

                // تسوية القسم
                if (dto.DepartmentId.HasValue && dto.DepartmentId > 0)
                {
                    employee.DepartmentId = dto.DepartmentId;
                }
                else if (!string.IsNullOrWhiteSpace(dto.DepartmentName))
                {
                    var deptMatch = await _context.Departments.FirstOrDefaultAsync(d =>
                        d.DepartmentName.ToLower() == dto.DepartmentName.Trim().ToLower() ||
                        d.DepartmentCode.ToLower() == dto.DepartmentName.Trim().ToLower());
                    if (deptMatch != null) employee.DepartmentId = deptMatch.Id;
                }

                // تسوية المسمى الوظيفي
                if (dto.JobTitleId.HasValue && dto.JobTitleId > 0)
                {
                    employee.JobTitleId = dto.JobTitleId;
                }
                else
                {
                    var jobText = !string.IsNullOrWhiteSpace(dto.JobTitle) ? dto.JobTitle : dto.JobTitleName;
                    if (!string.IsNullOrWhiteSpace(jobText))
                    {
                        var titleMatch = await _context.JobTitles.FirstOrDefaultAsync(j => j.TitleName.ToLower() == jobText.Trim().ToLower());
                        if (titleMatch != null)
                        {
                            employee.JobTitleId = titleMatch.Id;
                        }
                    }
                }

                employee.Active = dto.Active;

                await _context.SaveChangesAsync();
                return Ok(new { Message = "تم تحديث بيانات العامل بنجاح!" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"خطأ أثناء تحديث بيانات العامل: {ex.GetBaseException().Message}");
            }
        }

        // ==============================================================
        // 5. تبديل حالة العامل (نشط / موقوف) بنقرة زر
        // ==============================================================
        [HttpPut("{id:long}/toggle-status")]
        public async Task<IActionResult> ToggleStatus(long id)
        {
            try
            {
                var employee = await _context.Employees.FindAsync(id);
                if (employee == null) return NotFound("الموظف غير موجود.");

                employee.Active = !employee.Active;
                await _context.SaveChangesAsync();
                return Ok(new { Message = "تم تغيير حالة العامل بنجاح!", Active = employee.Active });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"خطأ أثناء تغيير الحالة: {ex.GetBaseException().Message}");
            }
        }

        // ==============================================================
        // 6. حذف العامل (محصن ضد انهيار القيود المفتاحية)
        // ==============================================================
        [HttpDelete("{id:long}")]
        public async Task<IActionResult> DeleteEmployee(long id)
        {
            try
            {
                var employee = await _context.Employees.FindAsync(id);
                if (employee == null) return NotFound("العامل غير موجود.");

                try
                {
                    _context.Employees.Remove(employee);
                    await _context.SaveChangesAsync();
                    return Ok(new { success = true, Message = "تم حذف العامل نهائياً بنجاح!" });
                }
                catch (DbUpdateException)
                {
                    // إذا كان مرتبطاً بسجلات تاريخية أو عهد أو أوامر تشغيل
                    employee.Active = false;
                    await _context.SaveChangesAsync();
                    return Ok(new { success = true, Message = "تم إيقاف وتعطيل العامل بدلاً من الحذف لوجود سجلات وعهد أو مهام مرتبطة به في النظام." });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"تعذر حذف العامل: {ex.GetBaseException().Message}");
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
        public string? FullName { get; set; }
        public string? EmployeeName { get; set; }
        public string? Code { get; set; }
        public string? WorkerCode { get; set; }
        public string? EmployeeCode { get; set; }
        public long? DepartmentId { get; set; }
        public string? DepartmentName { get; set; }
        public long? JobTitleId { get; set; }
        public string? JobTitle { get; set; }
        public string? JobTitleName { get; set; }
        public long? ManagerId { get; set; }
        public bool Active { get; set; } = true;
    }

    public class UpdateEmployeeDto
    {
        public string Name { get; set; } = string.Empty;
        public string? FullName { get; set; }
        public string? EmployeeName { get; set; }
        public string? Code { get; set; }
        public string? WorkerCode { get; set; }
        public string? EmployeeCode { get; set; }
        public long? DepartmentId { get; set; }
        public string? DepartmentName { get; set; }
        public long? JobTitleId { get; set; }
        public string? JobTitle { get; set; }
        public string? JobTitleName { get; set; }
        public bool Active { get; set; } = true;
    }
}