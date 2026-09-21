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
        // 1. مسار جلب الهيكل التنظيمي بالكامل (الشركة > الإدارات > الأقسام > العمال)
        // ==============================================================
        [HttpGet("organization-chart")]
        public async Task<IActionResult> GetOrganizationChart()
        {
            try
            {
                // جلب الإدارات الفعالة مرة واحدة
                var allDepartments = await _context.Departments
                    .Where(d => d.Active)
                    .ToListAsync();

                // جلب الموظفين الفعالين مع المسمى الوظيفي مرة واحدة
                var allEmployees = await _context.Employees
                    .Include(e => e.JobTitle)
                    .Where(e => e.Active)
                    .ToListAsync();

                // بناء الشجرة بدءاً من الإدارات الرئيسية (التي ليس لها أب)
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

        // دالة مساعدة (Recursive) لبناء شجرة الإدارات والأقسام
        private OrgDepartmentDto BuildDepartmentNode(Department dept, List<Department> allDepts, List<Employee> allEmps)
        {
            return new OrgDepartmentDto
            {
                Id = dept.Id,
                DepartmentName = dept.DepartmentName,

                // جلب الموظفين التابعين لهذا القسم، مرتبين حسب الدرجة الوظيفية (رئيس مجلس الإدارة/المدير أولاً)
                Employees = allEmps
                    .Where(e => e.DepartmentId == dept.Id && e.ManagerId == null)
                    .OrderBy(e => e.JobTitle?.JobGrade ?? 99)
                    .Select(e => BuildEmployeeNode(e, allEmps))
                    .ToList(),

                // التفرع للأقسام الداخلية
                SubDepartments = allDepts
                    .Where(d => d.ParentDepartmentId == dept.Id)
                    .Select(d => BuildDepartmentNode(d, allDepts, allEmps))
                    .ToList()
            };
        }

        // دالة مساعدة (Recursive) لبناء شجرة الموظفين (مدير > مشرف > عامل)
        private OrgEmployeeDto BuildEmployeeNode(Employee emp, List<Employee> allEmps)
        {
            return new OrgEmployeeDto
            {
                Id = emp.Id,
                Name = emp.Name,
                WorkerCode = emp.EmployeeCode,
                JobTitle = emp.JobTitle?.TitleName ?? "غير محدد",
                JobGrade = emp.JobTitle?.JobGrade ?? 99,

                // جلب الموظفين الذين يتبعون هذا الموظف كمدير مباشر لهم
                Subordinates = allEmps
                    .Where(e => e.ManagerId == emp.Id)
                    .OrderBy(e => e.JobTitle?.JobGrade ?? 99)
                    .Select(e => BuildEmployeeNode(e, allEmps))
                    .ToList()
            };
        }

        // ==============================================================
        // 2. العمليات الأساسية للموظفين (إضافة، جلب للقوائم المنسدلة)
        // ==============================================================
        [HttpGet]
        public async Task<IActionResult> GetEmployeesFlat()
        {
            var employees = await _context.Employees
                .Include(e => e.Department)
                .Include(e => e.JobTitle)
                .Select(e => new
                {
                    e.Id,
                    e.Name,
                    WorkerCode = e.EmployeeCode,
                    JobTitle = e.JobTitle != null ? e.JobTitle.TitleName : "غير محدد",
                    DepartmentName = e.Department != null ? e.Department.DepartmentName : "غير محدد",
                    e.Active
                })
                .ToListAsync();

            return Ok(employees);
        }

        [HttpPost]
        public async Task<IActionResult> CreateEmployee([FromBody] CreateEmployeeDto dto)
        {
            var companyId = (await _context.Companies.FirstOrDefaultAsync())?.Id ?? 1;

            var employee = new Employee
            {
                CompanyId = companyId,
                Name = dto.Name,
                EmployeeCode = dto.WorkerCode ?? string.Empty,
                DepartmentId = dto.DepartmentId > 0 ? dto.DepartmentId : null,
                JobTitleId = dto.JobTitleId > 0 ? dto.JobTitleId : null,
                ManagerId = dto.ManagerId > 0 ? dto.ManagerId : null,
                CreatedAt = DateTime.UtcNow,
                Active = true
            };

            _context.Employees.Add(employee);
            await _context.SaveChangesAsync();
            return Ok(new { Message = "تم حفظ العامل بنجاح!", EmployeeId = employee.Id });
        }
    }

    // ==============================================================
    // DTO Classes (مخصصة لنقل الشجرة بشكل نظيف للواجهة الأمامية)
    // ==============================================================
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

    public class CreateEmployeeDto
    {
        public string Name { get; set; } = string.Empty;
        public string WorkerCode { get; set; } = string.Empty;
        public long? DepartmentId { get; set; }
        public long? JobTitleId { get; set; }
        public long? ManagerId { get; set; }
    }
}