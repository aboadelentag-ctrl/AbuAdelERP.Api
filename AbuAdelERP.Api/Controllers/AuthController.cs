using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Linq;
using AbuAdelERP.Api.Models;
using AbuAdelERP.Api.DTOs;

namespace AbuAdelERP.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly AppDbContext _context;

        public AuthController(IConfiguration configuration, AppDbContext context)
        {
            _configuration = configuration;
            _context = context;
        }

        [HttpPost("login")]
        public IActionResult Login([FromBody] LoginDto request)
        {
            // 1. التأكد من وجود مستخدم مدير افتراضي
            if (!_context.Users.Any())
            {
                _context.Users.Add(new AppUser
                {
                    Username = "admin",
                    PasswordHash = "123456",
                    Role = "Admin",
                    FullName = "أحمد عبد العزيز (مدير النظام)",
                    DepartmentName = "الإدارة العليا والتشغيل",
                    IsActive = true
                });
                _context.SaveChanges();
            }

            // 2. التحقق من اسم المستخدم وكلمة المرور
            var user = _context.Users.FirstOrDefault(u => u.Username == request.Username && u.PasswordHash == request.Password);

            if (user == null)
            {
                return Unauthorized(new { Error = "اسم المستخدم أو كلمة المرور غير صحيحة." });
            }

            if (!user.IsActive)
            {
                return Unauthorized(new { Error = "هذا الحساب تم تجميده مؤقتاً، يرجى مراجعة إدارة النظام." });
            }

            // 3. احتساب حزمة الصلاحيات الفعلية (Effective Permissions)
            var effectivePermissions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            if (user.Role.Equals("Admin", StringComparison.OrdinalIgnoreCase))
            {
                // المدير له كافة الصلاحيات
                effectivePermissions.Add("ALL");
                foreach (var p in AppPermissions.GetAllPermissions())
                {
                    effectivePermissions.Add(p.Code);
                }
            }
            else
            {
                // جلب صلاحيات الدور المحدد للمستخدم
                var roleObj = _context.Roles.FirstOrDefault(r => r.Name == user.Role);
                if (roleObj != null && !string.IsNullOrWhiteSpace(roleObj.Permissions))
                {
                    if (roleObj.Permissions.Trim().Equals("ALL", StringComparison.OrdinalIgnoreCase))
                    {
                        foreach (var p in AppPermissions.GetAllPermissions()) effectivePermissions.Add(p.Code);
                    }
                    else
                    {
                        foreach (var code in roleObj.Permissions.Split(',', StringSplitOptions.RemoveEmptyEntries))
                        {
                            effectivePermissions.Add(code.Trim());
                        }
                    }
                }

                // إضافة أي استثناءات صلاحيات خاصة للمستخدم
                if (!string.IsNullOrWhiteSpace(user.CustomPermissions))
                {
                    foreach (var code in user.CustomPermissions.Split(',', StringSplitOptions.RemoveEmptyEntries))
                    {
                        effectivePermissions.Add(code.Trim());
                    }
                }
            }

            // 4. بناء الـ JWT Token محملاً بكل البيانات الرقابية
            string secretKey = _configuration["Jwt:Key"] ?? "AbuAdel_ERP_Super_Secret_Key_2027_Must_Be_Long_Enough";
            var keyBytes = Encoding.ASCII.GetBytes(secretKey);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Role, user.Role),
                new Claim("UserId", user.Id.ToString()),
                new Claim("FullName", user.FullName ?? user.Username),
                new Claim("DepartmentName", user.DepartmentName ?? "عام"),
                new Claim("DepartmentId", user.DepartmentId?.ToString() ?? "0")
            };

            // تضمين كل كود صلاحية كـ Claim مستقل داخل الـ Token
            foreach (var perm in effectivePermissions)
            {
                claims.Add(new Claim("Permission", perm));
            }

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddHours(12),
                Issuer = _configuration["Jwt:Issuer"],
                Audience = _configuration["Jwt:Audience"],
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(keyBytes), SecurityAlgorithms.HmacSha256Signature)
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.CreateToken(tokenDescriptor);

            // 5. إرجاع بيانات الجلسة كاملة للواجهة لتخزينها
            return Ok(new
            {
                Message = "تم تسجيل الدخول بنجاح",
                Token = tokenHandler.WriteToken(token),
                UserId = user.Id,
                Username = user.Username,
                FullName = user.FullName ?? user.Username,
                Role = user.Role,
                DepartmentName = user.DepartmentName ?? "عام",
                DepartmentId = user.DepartmentId,
                Permissions = effectivePermissions.ToList()
            });
        }
    }
}