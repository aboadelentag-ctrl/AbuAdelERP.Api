using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Linq;
using AbuAdelERP.Api.Models;
using AbuAdelERP.Api.DTOs;

namespace AbuAdelERP.Api.Controllers;

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
        // 1. إنشاء حساب مدير افتراضي لتجربة النظام إذا كان الجدول فارغاً
        if (!_context.Users.Any())
        {
            _context.Users.Add(new AppUser
            {
                Username = "admin",
                PasswordHash = "123456",
                Role = "Admin"
            });
            _context.SaveChanges();
        }

        // 2. التحقق من صحة بيانات الدخول
        var user = _context.Users.FirstOrDefault(u => u.Username == request.Username && u.PasswordHash == request.Password);

        if (user == null)
        {
            return Unauthorized(new { Error = "اسم المستخدم أو كلمة المرور غير صحيحة." });
        }

        // 3. جلب المفتاح السري وتحويله بشكل آمن
        string secretKey = _configuration["Jwt:Key"] ?? "AbuAdel_ERP_Super_Secret_Key_2027_Must_Be_Long_Enough";
        var keyBytes = Encoding.ASCII.GetBytes(secretKey);

        // 4. بناء تصريح المرور (Token)
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Role, user.Role),
                new Claim("UserId", user.Id.ToString())
            }),
            Expires = DateTime.UtcNow.AddHours(8),
            Issuer = _configuration["Jwt:Issuer"],
            Audience = _configuration["Jwt:Audience"],
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(keyBytes), SecurityAlgorithms.HmacSha256Signature)
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);

        // 5. إرجاع التوكن للمستخدم
        return Ok(new
        {
            Message = "تم تسجيل الدخول بنجاح",
            Token = tokenHandler.WriteToken(token),
            Role = user.Role
        });
    }
}