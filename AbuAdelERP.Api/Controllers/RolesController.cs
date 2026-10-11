using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AbuAdelERP.Api.Models;

namespace AbuAdelERP.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RolesController : ControllerBase
    {
        private readonly AppDbContext _context;

        public RolesController(AppDbContext context)
        {
            _context = context;
        }

        // 1. استرجاع فهرس الصلاحيات الكامل
        [HttpGet("catalog")]
        public IActionResult GetPermissionsCatalog()
        {
            return Ok(AppPermissions.Catalog);
        }

        // 2. استرجاع أدوار المنشأة
        [HttpGet("company/{companyId}")]
        public async Task<IActionResult> GetRolesByCompany(long companyId)
        {
            var roles = await _context.Roles
                .Where(r => r.CompanyId == (int)companyId || r.CompanyId == null || r.CompanyId == 1)
                .OrderBy(r => r.Id)
                .Select(r => new
                {
                    r.Id,
                    r.Name,
                    r.DisplayName,
                    r.Description,
                    r.Permissions,
                    r.Active,
                    UsersCount = _context.Users.Count(u => u.Role == r.Name || u.Role == r.DisplayName)
                })
                .ToListAsync();

            return Ok(roles);
        }

        // 3. إنشاء مصفوفة ودور وظيفي جديد من الواجهة
        [HttpPost]
        public async Task<IActionResult> CreateRole([FromBody] CreateRoleDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.DisplayName))
                return BadRequest("اسم الدور الوظيفي مطلوب.");

            string roleCode = string.IsNullOrWhiteSpace(dto.Name)
                ? "ROLE_" + Guid.NewGuid().ToString("N")[..6].ToUpper()
                : dto.Name.Trim().ToUpper();

            var newRole = new AppRole
            {
                Name = roleCode,
                DisplayName = dto.DisplayName.Trim(),
                Description = dto.Description,
                CompanyId = (int)dto.CompanyId,
                Permissions = string.Join(",", dto.Permissions ?? new List<string>()),
                Active = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Roles.Add(newRole);
            await _context.SaveChangesAsync();

            return Ok(new { success = true, roleId = newRole.Id, message = "تم إنشاء الدور والمصفوفة بنجاح." });
        }

        // 4. تحديث مصفوفة صلاحيات الدور
        [HttpPut("{roleId}/permissions")]
        public async Task<IActionResult> UpdateRolePermissions(int roleId, [FromBody] UpdateRolePermissionsDto dto)
        {
            var role = await _context.Roles.FindAsync(roleId);
            if (role == null) return NotFound("الدور غير مسجل.");

            role.Permissions = string.Join(",", dto.Permissions.Distinct());
            role.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return Ok(new { success = true, message = "تم حفظ مصفوفة صلاحيات الدور بنجاح." });
        }

        // 5. جلب قائمة العاملين بالمنشأة لربطهم بحسابات المستخدمين
        [HttpGet("employees/{companyId}")]
        public async Task<IActionResult> GetEmployeesLookup(long companyId)
        {
            var employees = await _context.Employees
                .Where(e => (e.CompanyId == companyId || e.CompanyId == null) && e.Active)
                .Select(e => new
                {
                    e.Id,
                    e.EmployeeCode,
                    FullName = e.EmployeeCode,
                    DepartmentName = e.Department != null ? e.Department.DepartmentName : "عام"
                })
                .ToListAsync();

            return Ok(employees);
        }

        // 6. استرجاع مستخدمي المنشأة
        [HttpGet("users/{companyId}")]
        public async Task<IActionResult> GetCompanyUsers(long companyId)
        {
            var users = await _context.Users
                .Where(u => u.CompanyId == companyId || u.CompanyId == null)
                .OrderBy(u => u.Id)
                .Select(u => new
                {
                    u.Id,
                    u.Username,
                    u.FullName,
                    u.EmployeeId,
                    u.EmployeeCode,
                    u.Role,
                    u.RoleId,
                    u.DepartmentName,
                    u.IsActive,
                    u.CustomPermissions
                })
                .ToListAsync();

            return Ok(users);
        }

        // 7. إنشاء مستخدم جديد وربطه بالعامل
        [HttpPost("users")]
        public async Task<IActionResult> CreateUser([FromBody] CreateUserDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Username) || string.IsNullOrWhiteSpace(dto.Password))
                return BadRequest("اسم المستخدم وكلمة المرور مطلوبان.");

            bool exists = await _context.Users.AnyAsync(u => u.CompanyId == dto.CompanyId && u.Username.ToLower() == dto.Username.Trim().ToLower());
            if (exists)
                return BadRequest("اسم المستخدم مسجل مسبقاً لهذه المنشأة.");

            var newUser = new AppUser
            {
                Username = dto.Username.Trim(),
                PasswordHash = dto.Password.Trim(),
                FullName = dto.FullName ?? dto.Username,
                CompanyId = dto.CompanyId,
                RoleId = dto.RoleId,
                Role = dto.RoleName ?? "User",
                DepartmentName = dto.DepartmentName,
                EmployeeId = dto.EmployeeId,
                EmployeeCode = dto.EmployeeCode,
                IsActive = true,
                CustomPermissions = ""
            };

            _context.Users.Add(newUser);
            await _context.SaveChangesAsync();

            return Ok(new { success = true, userId = newUser.Id, message = "تم إنشاء حساب المستخدم بنجاح." });
        }

        // 8. تحديث حالة المستخدم أو كلمة المرور
        [HttpPut("users/{userId}/status")]
        public async Task<IActionResult> UpdateUserStatus(long userId, [FromBody] UpdateUserStatusDto dto)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return NotFound("المستخدم غير موجود.");

            user.IsActive = dto.IsActive;
            if (!string.IsNullOrWhiteSpace(dto.NewPassword))
            {
                user.PasswordHash = dto.NewPassword.Trim();
            }

            await _context.SaveChangesAsync();
            return Ok(new { success = true, message = "تم تحديث بيانات الحساب بنجاح." });
        }

        // 9. حفظ الصلاحيات الاستثنائية للمستخدم
        [HttpPut("users/{userId}/custom-permissions")]
        public async Task<IActionResult> UpdateUserCustomPermissions(long userId, [FromBody] UpdateUserPermissionsDto dto)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return NotFound("المستخدم غير موجود.");

            user.CustomPermissions = string.Join(",", dto.Permissions.Distinct());
            await _context.SaveChangesAsync();
            return Ok(new { success = true, message = "تم حفظ الصلاحيات الاستثنائية للمستخدم بنجاح." });
        }
    }

    public class CreateRoleDto
    {
        public string Name { get; set; } = "";
        public string DisplayName { get; set; } = "";
        public string? Description { get; set; }
        public long CompanyId { get; set; }
        public List<string> Permissions { get; set; } = new();
    }

    public class UpdateRolePermissionsDto
    {
        public List<string> Permissions { get; set; } = new();
    }

    public class CreateUserDto
    {
        public string Username { get; set; } = "";
        public string Password { get; set; } = "";
        public string? FullName { get; set; }
        public long CompanyId { get; set; }
        public long? RoleId { get; set; }
        public string? RoleName { get; set; }
        public string? DepartmentName { get; set; }
        public long? EmployeeId { get; set; }
        public string? EmployeeCode { get; set; }
    }

    public class UpdateUserStatusDto
    {
        public bool IsActive { get; set; }
        public string? NewPassword { get; set; }
    }

    public class UpdateUserPermissionsDto
    {
        public List<string> Permissions { get; set; } = new();
    }
}