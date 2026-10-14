using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
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
        private readonly IRefreshTokenService _refreshTokenService;

        public AuthController(
            AppDbContext context,
            IJwtService jwtService,
            IPasswordHasher passwordHasher,
            IRefreshTokenService refreshTokenService)
        {
            _context = context;
            _jwtService = jwtService;
            _passwordHasher = passwordHasher;
            _refreshTokenService = refreshTokenService;
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
        // 2. دخول المستخدم + إصدار Access + Refresh Tokens
        // ==========================================================
        [HttpPost("login-user")]
        public async Task<IActionResult> LoginUser([FromBody] UserTenantLoginRequest dto)
        {
            if (dto.CompanyId <= 0 ||
                string.IsNullOrWhiteSpace(dto.Username) ||
                string.IsNullOrWhiteSpace(dto.Password))
                return BadRequest("بيانات تسجيل الدخول غير مكتملة.");

            var usernameNormalized = dto.Username.Trim().ToLower();

            var user = await _context.Users
                .FirstOrDefaultAsync(u =>
                    u.CompanyId == dto.CompanyId &&
                    u.Username.ToLower() == usernameNormalized);

            if (user == null)
                return Unauthorized("اسم المستخدم أو كلمة المرور غير صحيحة لهذه المنشأة.");

            // ===== التحقق من الباسورد (مع Auto-migration) =====
            bool passwordValid = false;
            bool needsMigration = false;

            bool isBcryptHash = user.PasswordHash.StartsWith("$2");

            if (isBcryptHash)
            {
                passwordValid = _passwordHasher.Verify(dto.Password.Trim(), user.PasswordHash);
                needsMigration = _passwordHasher.NeedsRehash(user.PasswordHash);
            }
            else
            {
                passwordValid = user.PasswordHash == dto.Password.Trim();
                if (passwordValid)
                {
                    user.PasswordHash = _passwordHasher.Hash(dto.Password.Trim());
                    needsMigration = false;
                }
            }

            if (!passwordValid)
                return Unauthorized("اسم المستخدم أو كلمة المرور غير صحيحة لهذه المنشأة.");

            if (!user.IsActive)
                return Unauthorized("حساب هذا المستخدم معطل حالياً من قبل مدير المنشأة.");

            // ===== جلب الصلاحيات =====
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

            string userRoleName = roleEntity?.DisplayName ?? user.Role ?? "مستخدم";

            if (userRoleName.Contains("مدير") ||
                userRoleName.Equals("Admin", StringComparison.OrdinalIgnoreCase) ||
                userRoleName.Equals("SuperAdmin", StringComparison.OrdinalIgnoreCase))
            {
                permissions.Add("*");
            }

            // ===== حفظ التغييرات (Password Migration) =====
            if (needsMigration || _context.ChangeTracker.HasChanges())
            {
                await _context.SaveChangesAsync();
            }

            // ===== بيانات الشركة =====
            var company = await _context.Companies
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == dto.CompanyId);

            var companyIdValue = user.CompanyId ?? dto.CompanyId;

            // ===== إصدار Access Token =====
            var accessToken = _jwtService.GenerateAccessToken(
                userId: user.Id,
                username: user.Username,
                companyId: companyIdValue,
                role: userRoleName,
                permissions: permissions,
                branchId: null);

            // ===== إصدار Refresh Token =====
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            var refreshToken = await _refreshTokenService.CreateAsync(user.Id, ipAddress);

            // ===== Response =====
            return Ok(new
            {
                success = true,
                token = accessToken,
                refreshToken = refreshToken.Token,
                tokenType = "Bearer",
                expiresIn = 480 * 60, // بالثواني
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

        // ==========================================================
        // 3. تجديد Access Token باستخدام Refresh Token
        // ==========================================================
        [HttpPost("refresh")]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest dto)
        {
            if (string.IsNullOrWhiteSpace(dto.RefreshToken))
                return BadRequest("Refresh token is required.");

            try
            {
                // 1. التحقق من الـ Refresh Token
                var existingToken = await _refreshTokenService.ValidateAsync(dto.RefreshToken);

                // 2. جلب المستخدم
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Id == existingToken.UserId);

                if (user == null || !user.IsActive)
                    return Unauthorized("User account is no longer active.");

                // 3. جلب صلاحيات الدور
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

                string userRoleName = roleEntity?.DisplayName ?? user.Role ?? "مستخدم";

                if (userRoleName.Contains("مدير") ||
                    userRoleName.Equals("Admin", StringComparison.OrdinalIgnoreCase) ||
                    userRoleName.Equals("SuperAdmin", StringComparison.OrdinalIgnoreCase))
                {
                    permissions.Add("*");
                }

                var companyIdValue = user.CompanyId ?? 1;

                // 4. إصدار Access Token جديد
                var newAccessToken = _jwtService.GenerateAccessToken(
                    userId: user.Id,
                    username: user.Username,
                    companyId: companyIdValue,
                    role: userRoleName,
                    permissions: permissions,
                    branchId: null);

                // 5. إلغاء الـ Refresh Token القديم
                var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                var newRefreshToken = await _refreshTokenService.CreateAsync(user.Id, ipAddress);

                await _refreshTokenService.RevokeAsync(
                    existingToken,
                    reason: "Replaced by new token",
                    replacedByToken: newRefreshToken.Token);

                // 6. Response
                return Ok(new
                {
                    success = true,
                    token = newAccessToken,
                    refreshToken = newRefreshToken.Token,
                    tokenType = "Bearer",
                    expiresIn = 480 * 60
                });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(ex.Message);
            }
        }

        // ==========================================================
        // 4. تسجيل الخروج — إلغاء Refresh Token
        // ==========================================================
        [HttpPost("logout")]
        public async Task<IActionResult> Logout([FromBody] RefreshTokenRequest dto)
        {
            if (string.IsNullOrWhiteSpace(dto.RefreshToken))
                return BadRequest("Refresh token is required.");

            try
            {
                var token = await _refreshTokenService.ValidateAsync(dto.RefreshToken);
                await _refreshTokenService.RevokeAsync(token, reason: "Logout");

                return Ok(new
                {
                    success = true,
                    message = "تم تسجيل الخروج بنجاح."
                });
            }
            catch (UnauthorizedAccessException)
            {
                // حتى لو التوكن مش صالح، نرجّع success (idempotent)
                return Ok(new
                {
                    success = true,
                    message = "تم تسجيل الخروج."
                });
            }
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

    public class RefreshTokenRequest
    {
        public string RefreshToken { get; set; } = string.Empty;
    }
}