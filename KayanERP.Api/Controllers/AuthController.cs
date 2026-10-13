using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KayanERP.Api.Models;
using KayanERP.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KayanERP.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [AllowAnonymous]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IJwtService _jwtService;
        private readonly IPasswordHasher _passwordHasher;

        public AuthController(
            AppDbContext context,
            IJwtService jwtService,
            IPasswordHasher passwordHasher)
        {
            _context = context;
            _jwtService = jwtService;
            _passwordHasher = passwordHasher;
        }

        // ==========================================================
        // 1. التحقق من ترخيص الشركة المستأجرة
        // ==========================================================
        [HttpPost("verify-tenant")]
        public async Task<IActionResult> VerifyTenant([FromBody] TenantVerifyRequest dto)
        {
            if (string.IsNullOrWhiteSpace(dto.TenantCode) ||
                string.IsNullOrWhiteSpace(dto.TenantKey))
                return BadRequest("يرجى إدخال كود المنشأة ومفتاح الدخول الخاص بها.");

            var tenantCodeNormalized = dto.TenantCode.Trim().ToLower();

            var company = await _context.Companies
                .AsNoTracking()
                .FirstOrDefaultAsync(c =>
                    (c.TenantCode != null && c.TenantCode.ToLower() == tenantCodeNormalized) ||
                    c.CompanyName.ToLower() == tenantCodeNormalized);

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
        // 2. دخول المستخدم + إصدار JWT Token
        // ==========================================================
        [HttpPost("login-user")]
        public async Task<IActionResult> LoginUser([FromBody] UserTenantLoginRequest dto)
        {
            // ===== 1. التحقق من المدخلات =====
            if (dto.CompanyId <= 0 ||
                string.IsNullOrWhiteSpace(dto.Username) ||
                string.IsNullOrWhiteSpace(dto.Password))
                return BadRequest("بيانات تسجيل الدخول غير مكتملة.");

            var usernameNormalized = dto.Username.Trim().ToLower();

            // ===== 2. البحث عن المستخدم =====
            var user = await _context.Users
                .FirstOrDefaultAsync(u =>
                    u.CompanyId == dto.CompanyId &&
                    u.Username.ToLower() == usernameNormalized);

            if (user == null)
                return Unauthorized("اسم المستخدم أو كلمة المرور غير صحيحة لهذه المنشأة.");

            // ===== 3. التحقق من الباسورد (مع Auto-migration) =====
            bool passwordValid = false;
            bool needsMigration = false;

            // هل الـ hash مخزن كـ BCrypt (يبدأ بـ $2a$ / $2b$ / $2y$)?
            bool isBcryptHash = user.PasswordHash.StartsWith("$2");

            if (isBcryptHash)
            {
                // ✅ الحالة الطبيعية — مقارنة BCrypt
                passwordValid = _passwordHasher.Verify(dto.Password.Trim(), user.PasswordHash);
                needsMigration = _passwordHasher.NeedsRehash(user.PasswordHash);
            }
            else
            {
                // ⚠️ باسورد قديم مخزن نص صريح — نقارن مباشر
                passwordValid = user.PasswordHash == dto.Password.Trim();

                // لو صح، نعمل Migration تلقائي لباسورد مشفّر
                if (passwordValid)
                {
                    user.PasswordHash = _passwordHasher.Hash(dto.Password.Trim());
                    needsMigration = false; // عملنا الـ migration دلوقتي
                }
            }

            if (!passwordValid)
                return Unauthorized("اسم المستخدم أو كلمة المرور غير صحيحة لهذه المنشأة.");

            if (!user.IsActive)
                return Unauthorized("حساب هذا المستخدم معطل حالياً من قبل مدير المنشأة.");

            // ===== 4. جلب صلاحيات الدور =====
            var roleEntity = await _context.Roles
                .AsNoTracking()
                .FirstOrDefaultAsync(r =>
                    r.Name == user.Role ||
                    r.DisplayName == user.Role);

            var permissions = new List<string>();

            if (roleEntity != null && !string.IsNullOrWhiteSpace(roleEntity.Permissions))
            {
                permissions.AddRange(roleEntity.Permissions
                    .Split(new[] { ',', ';', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(p => p.Trim()));
            }

            if (!string.IsNullOrWhiteSpace(user.CustomPermissions))
            {
                permissions.AddRange(user.CustomPermissions
                    .Split(new[] { ',', ';', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(p => p.Trim()));
            }

            permissions = permissions
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            // ===== 5. التحقق من صلاحيات المدير =====
            string userRoleName = roleEntity?.DisplayName ?? user.Role ?? "مستخدم";

            if (userRoleName.Contains("مدير") ||
                userRoleName.Equals("Admin", StringComparison.OrdinalIgnoreCase) ||
                userRoleName.Equals("SuperAdmin", StringComparison.OrdinalIgnoreCase))
            {
                permissions.Add("*");
            }

            // ===== 6. حفظ أي تغييرات (مثل migration الباسورد) =====
            if (needsMigration || _context.ChangeTracker.HasChanges())
            {
                await _context.SaveChangesAsync();
            }

            // ===== 7. بيانات الشركة =====
            var company = await _context.Companies
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == dto.CompanyId);

            var companyIdValue = user.CompanyId ?? dto.CompanyId;

            // ===== 8. إصدار الـ JWT Token =====
            var token = _jwtService.GenerateAccessToken(
                userId: user.Id,
                username: user.Username,
                companyId: companyIdValue,
                role: userRoleName,
                permissions: permissions,
                branchId: null);

            // ===== 9. الرجوع بالنتيجة =====
            return Ok(new
            {
                success = true,
                token = token,
                tokenType = "Bearer",
                userId = user.Id,
                username = user.Username,
                fullName = user.FullName ?? user.Username,
                companyId = companyIdValue,
                companyName = company?.CompanyName ?? "المنشأة المستأجرة",
                role = userRoleName,
                employeeCode = user.EmployeeCode ?? "",
                departmentName = user.DepartmentName ?? "الإدارة العامة",
                permissions = permissions
            });
        }
    }

    // ===== DTOs =====
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