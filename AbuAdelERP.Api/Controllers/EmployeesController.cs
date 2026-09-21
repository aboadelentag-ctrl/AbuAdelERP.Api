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
        // 1. إدارة المسميات الوظيفية (جلب + إضافة سريعة)
        // ==============================================================
        [HttpGet("job-titles")]
        public async Task<IActionResult> GetJobTitles()
        {
            try
            {
                var titles = await _context.JobTitles
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
                return StatusCode(500, $"خطأ في جلب الوظائف: {ex.Message}");
            }
        }

        [HttpPost("job-titles")]
        public async Task<IActionResult> CreateJobTitle([FromBody] CreateJobTitleDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.TitleName))
            {
                return BadRequest("يرجى إدخال اسم المسمى الوظيفي.");
            }

            try
            {
                var companyId = (await _context.Companies.FirstOrDefaultAsync())?.Id ?? 1;

                var jobTitle = new JobTitle
                {
                    CompanyId = companyId,
                    TitleName = dto.TitleName.Trim(),
                    JobGrade = dto.JobGrade > 0 ? dto.JobGrade : 10,
                    Active = true
                };

                _context.JobTitles.Add(jobTitle);
                await _context.SaveChangesAsync();

                return Ok(new { Message = "تم إضافة المسمى الوظيفي بنجاح!", Id = jobTitle.Id, TitleName = jobTitle.TitleName });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"خطأ في حفظ المسمى الوظيفي: {ex.Message}");
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
                    .Include(e => e.Department)
                    .Include(e => e.JobTitle)
                    .Select(e => new
                    {
                        e.Id,
                        e.Name,
                        WorkerCode = e.EmployeeCode,
                        JobTitleId = e.JobTitleId,
                        JobTitle = e.JobTitle != null ? e.JobTitle.TitleName : "غير محدد",
                        DepartmentId = e.DepartmentId,
                        DepartmentName = e.Department != null ? e.Department.DepartmentName : "غير محدد",
                        e.Active
                    })
                    .ToListAsync();

                return Ok(employees);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"خطأ في جلب الموظفين: {ex.Message}");
            }
        }

        // ==============================================================
        // 3. إضافة موظف جديد
        // ==============================================================
        [HttpPost]
        public async Task<IActionResult> CreateEmployee([FromBody] CreateEmployeeDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Name) || !dto.DepartmentId.HasValue || dto.DepartmentId <= 0)
            {
                return BadRequest("اسم العامل والقسم مطلوبان.");
            }

            try
            {
                var companyId = (await _context.Companies.FirstOrDefaultAsync())?.Id ?? 1;

                var employee = new Employee
                {
                    CompanyId = companyId,
                    Name = dto.Name.Trim(),
                    EmployeeCode = dto.WorkerCode?.Trim() ?? string.Empty,
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
                return StatusCode(500, $"خطأ أثناء حفظ العامل: {ex.Message}");
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

                employee.Name = dto.Name.Trim();
                employee.EmployeeCode = dto.WorkerCode?.Trim() ?? string.Empty;
                employee.DepartmentId = dto.DepartmentId > 0 ? dto.DepartmentId : null;
                employee.JobTitleId = dto.JobTitleId > 0 ? dto.JobTitleId : null;
                employee.Active = dto.Active;

                await _context.SaveChangesAsync();
                return Ok(new { Message = "تم تحديث بيانات العامل بنجاح!" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"خطأ أثناء تحديث بيانات العامل: {ex.Message}");
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
        // 6. مسار الهيكل التنظيمي الشجري (الشركة > الإدارات > الأقسام)
        // ==============================================================
        [HttpGet("organization-chart")]
        public async Task<IActionResult> GetOrganizationChart()
        {
            try
            {
                var allDepartments = await _context.Departments
                    .Where(d => d.Active)
                    .ToListAsync();

                var allEmployees = await _context.Employees
                    .Include(e => e.JobTitle)
                    .Where(e => e.Active)
                    .ToListAsync();

                var rootDepartments = allDepartments
                    .Where(d => d.ParentDepartmentId == null)
                    .Select(d => BuildDepartmentNode(d, allDepartments, allEmployees))
                    .ToList();

                return Ok(rootDepartments);
            }
            catch (Exception ex)
            {
                var errorMsg = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                return StatusCode(500, $"حدث خطأ أثناء بناء الهيكل التنظيمي: {errorMsg}");
            }
        }

        private OrgDepartmentDto BuildDepartmentNode(Department dept, List<Department> allDepts, List<Employee> allEmps)
        {
            return new OrgDepartmentDto
            {
                Id = dept.Id,
                DepartmentName = dept.DepartmentName,
                Employees = allEmps
                    .Where(e => e.DepartmentId == dept.Id && e.ManagerId == null)
                    .OrderBy(e => e.JobTitle?.JobGrade ?? 99)
                    .Select(e => BuildEmployeeNode(e, allEmps))
                    .ToList(),
                SubDepartments = allDepts
                    .Where(d => d.ParentDepartmentId == dept.Id)
                    .Select(d => BuildDepartmentNode(d, allDepts, allEmps))
                    .ToList()
            };
        }

        private OrgEmployeeDto BuildEmployeeNode(Employee emp, List<Employee> allEmps)
        {
            return new OrgEmployeeDto
            {
                Id = emp.Id,
                Name = emp.Name,
                WorkerCode = emp.EmployeeCode,
                JobTitle = emp.JobTitle?.TitleName ?? "غير محدد",
                JobGrade = emp.JobTitle?.JobGrade ?? 99,
                Subordinates = allEmps
                    .Where(e => e.ManagerId == emp.Id)
                    .OrderBy(e => e.JobTitle?.JobGrade ?? 99)
                    .Select(e => BuildEmployeeNode(e, allEmps))
                    .ToList()
            };
        }
    }

    // ==============================================================
    // نماذج البيانات (DTOs)
    // ==============================================================
    public class JobTitleLookupDto
    {
        public long Id { get; set; }
        public string TitleName { get; set; } = string.Empty;
    }

    public class CreateJobTitleDto
    {
        public string TitleName { get; set; } = string.Empty;
        public int JobGrade { get; set; } = 10;
    }

    public class CreateEmployeeDto
    {
        public string Name { get; set; } = string.Empty;
        public string WorkerCode { get; set; } = string.Empty;
        public long? DepartmentId { get; set; }
        public long? JobTitleId { get; set; }
        public long? ManagerId { get; set; }
        public bool Active { get; set; } = true;
    }

    public class UpdateEmployeeDto
    {
        public string Name { get; set; } = string.Empty;
        public string WorkerCode { get; set; } = string.Empty;
        public long? DepartmentId { get; set; }
        public long? JobTitleId { get; set; }
        public bool Active { get; set; } = true;
    }

    public class OrgDepartmentDto
    {
        public long Id { get; set; }
        public string DepartmentName { get; set; } = string.Empty;
        public List<OrgDepartmentDto> SubDepartments { get; set; } = new();
        public List<OrgEmployeeDto> Employees { get; set; } = new();
    }

    public class OrgEmployeeDto
    {
        public long Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string WorkerCode { get; set; } = string.Empty;
        public string JobTitle { get; set; } = string.Empty;
        public int JobGrade { get; set; }
        public List<OrgEmployeeDto> Subordinates { get; set; } = new();
    }
}