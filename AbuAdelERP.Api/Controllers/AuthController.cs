using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
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
            if (request == null || string.IsNullOrWhiteSpace(request.Username))
            {
                return BadRequest(new { Error = "يرجى إدخال اسم المستخدم وكلمة المرور." });
            }

            var cleanUser = request.Username.Trim();
            var cleanPass = request.Password?.Trim() ?? string.Empty;

            // استعلام مباشر ومرن يتجاهل حالة الأحرف في اسم المستخدم
            var user = _context.Users
                .Where(u => u.Username.ToLower() == cleanUser.ToLower() && u.PasswordHash == cleanPass)
                .Select(u => new
                {
                    u.Id,
                    u.Username,
                    u.FullName,
                    u.Role,
                    u.DepartmentName,
                    u.DepartmentId,
                    u.IsActive,
                    u.CustomPermissions
                })
                .FirstOrDefault();

            if (user == null)
            {
                return Unauthorized(new { Error = "اسم المستخدم أو كلمة المرور غير صحيحة." });
            }

            if (!user.IsActive)
            {
                return Unauthorized(new { Error = "هذا الحساب تم تجميده مؤقتاً، يرجى مراجعة إدارة النظام." });
            }

            var effectivePermissions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            if (string.Equals(user.Role, "Admin", StringComparison.OrdinalIgnoreCase))
            {
                effectivePermissions.Add("ALL");
                foreach (var p in AppPermissions.GetAllPermissions())
                {
                    effectivePermissions.Add(p.Code);
                }
            }
            else
            {
                var roleObj = _context.Roles.FirstOrDefault(r => r.Name == user.Role);
                if (roleObj != null && !string.IsNullOrWhiteSpace(roleObj.Permissions))
                {
                    if (roleObj.Permissions.Trim().Equals("ALL", StringComparison.OrdinalIgnoreCase))
                    {
                        foreach (var p in AppPermissions.GetAllPermissions())
                        {
                            effectivePermissions.Add(p.Code);
                        }
                    }
                    else
                    {
                        foreach (var code in roleObj.Permissions.Split(',', StringSplitOptions.RemoveEmptyEntries))
                        {
                            effectivePermissions.Add(code.Trim());
                        }
                    }
                }

                if (!string.IsNullOrWhiteSpace(user.CustomPermissions))
                {
                    foreach (var code in user.CustomPermissions.Split(',', StringSplitOptions.RemoveEmptyEntries))
                    {
                        effectivePermissions.Add(code.Trim());
                    }
                }
            }

            string secretKey = _configuration["Jwt:Key"] ?? "AbuAdelSuperSecretKeyForJwtAuthentication2026";
            var keyBytes = Encoding.ASCII.GetBytes(secretKey);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Role, user.Role),
                new Claim("UserId", user.Id.ToString()),
                new Claim("FullName", user.FullName ?? user.Username),
                new Claim("DepartmentName", user.DepartmentName ?? "الإدارة العامة"),
                new Claim("DepartmentId", user.DepartmentId?.ToString() ?? "0")
            };

            foreach (var perm in effectivePermissions)
            {
                claims.Add(new Claim("Permission", perm));
            }

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddHours(12),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(keyBytes), SecurityAlgorithms.HmacSha256Signature)
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.CreateToken(tokenDescriptor);

            return Ok(new
            {
                Message = "تم تسجيل الدخول بنجاح",
                Token = tokenHandler.WriteToken(token),
                UserId = user.Id,
                Username = user.Username,
                FullName = user.FullName ?? user.Username,
                Role = user.Role,
                DepartmentName = user.DepartmentName ?? "الإدارة العامة",
                DepartmentId = user.DepartmentId,
                Permissions = effectivePermissions.ToList()
            });
        }
    }
}