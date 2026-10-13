using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using KayanERP.Api.Models;

namespace KayanERP.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _context;

        public AuthController(AppDbContext context)
        {
            _context = context;
        }

        // ==========================================================
        // 1. المرحلة الأولى: التحقق من ترخيص وبيانات الشركة المستأجرة
        // ==========================================================
        [HttpPost("verify-tenant")]
        public async Task<IActionResult> VerifyTenant([FromBody] TenantVerifyRequest dto)
        {
            if (string.IsNullOrWhiteSpace(dto.TenantCode) || string.IsNullOrWhiteSpace(dto.TenantKey))
                return BadRequest("يرجى إدخال كود المنشأة ومفتاح الدخول الخاص بها.");

            var company = await _context.Companies
                .AsNoTracking()
                .FirstOrDefaultAsync(c =>
                    (c.TenantCode != null && c.TenantCode.ToLower() == dto.TenantCode.Trim().ToLower()) ||
                    c.CompanyName.ToLower() == dto.TenantCode.Trim().ToLower());

            if (company == null)
                return NotFound("المنشأة المستأجرة غير مسجلة بالنظام السحابي.");

            if (!company.Active)
                return Unauthorized("اشتراك هذه المنشأة موقوف حالياً، يرجى مراجعة إدارة منصة كيان.");

            if (company.TenantKey != dto.TenantKey.Trim())
                return Unauthorized("مفتاح الدخول السري للمنشأة غير صحيح.");

            return Ok(new
            {
                success = true,
                companyId = company.Id,
                companyName = company.CompanyName,
                tenantCode = company.TenantCode ?? "TENANT-01",
                message = "تم تأكيد هوية المنشأة بنجاح."
            });
        }

        // ==========================================================
        // 2. المرحلة الثانية: دخول المستخدم التابع للمنشأة المستأجرة
        // ==========================================================
        [HttpPost("login-user")]
        public async Task<IActionResult> LoginUser([FromBody] UserTenantLoginRequest dto)
        {
            if (dto.CompanyId <= 0 || string.IsNullOrWhiteSpace(dto.Username) || string.IsNullOrWhiteSpace(dto.Password))
                return BadRequest("بيانات تسجيل الدخول غير مكتملة.");

            // 1. البحث عن المستخدم داخل نطاق الشركة المعتمدة
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.CompanyId == dto.CompanyId && u.Username.ToLower() == dto.Username.Trim().ToLower());

            if (user == null || user.PasswordHash != dto.Password.Trim())
                return Unauthorized("اسم المستخدم أو كلمة المرور غير صحيحة لهذه المنشأة.");

            if (!user.IsActive)
                return Unauthorized("حساب هذا المستخدم معطل حالياً من قبل مدير المنشأة.");

            // 2. البحث عن بيانات وصلاحيات الدور من جدول الأدوار (Roles)
            var roleEntity = await _context.Roles
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.Name == user.Role || r.DisplayName == user.Role);

            var permissions = new List<string>();

            // استخراج الصلاحيات المسندة للدور
            if (roleEntity != null && !string.IsNullOrWhiteSpace(roleEntity.Permissions))
            {
                permissions.AddRange(roleEntity.Permissions
                    .Split(new[] { ',', ';', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(p => p.Trim()));
            }

            // استخراج أي صلاحيات فردية خاصة مسندة للمستخدم مباشرة (CustomPermissions)
            if (!string.IsNullOrWhiteSpace(user.CustomPermissions))
            {
                permissions.AddRange(user.CustomPermissions
                    .Split(new[] { ',', ';', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(p => p.Trim()));
            }

            permissions = permissions.Distinct(StringComparer.OrdinalIgnoreCase).ToList();

            // التحقق من صلاحيات المدير الشاملة
            string userRoleName = roleEntity?.DisplayName ?? user.Role ?? "مستخدم";
            if (userRoleName.Contains("مدير") ||
                userRoleName.Equals("Admin", StringComparison.OrdinalIgnoreCase) ||
                userRoleName.Equals("SuperAdmin", StringComparison.OrdinalIgnoreCase))
            {
                permissions.Add("*");
            }

            var company = await _context.Companies.FindAsync(dto.CompanyId);

            return Ok(new
            {
                success = true,
                userId = user.Id,
                username = user.Username,
                fullName = user.FullName ?? user.Username,
                companyId = user.CompanyId ?? dto.CompanyId,
                companyName = company?.CompanyName ?? "المنشأة المستأجرة",
                role = userRoleName,
                employeeCode = "",
                departmentName = user.DepartmentName ?? "الإدارة العامة",
                permissions = permissions
            });
        }
    }

    public class TenantVerifyRequest
    {
        public string TenantCode { get; set; } = string.Empty;
        public string TenantKey { get; set; } = string.Empty;
    }

    public class UserTenantLoginRequest
    {
        public long CompanyId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
}