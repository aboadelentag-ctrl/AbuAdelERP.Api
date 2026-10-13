using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace KayanERP.Api.Services;

/// <summary>
/// واجهة خدمة إصدار الـ JWT Tokens.
/// </summary>
public interface IJwtService
{
    /// <summary>
    /// يولّد Access Token للمستخدم بناءً على بياناته وصلاحياته.
    /// </summary>
    string GenerateAccessToken(
        long userId,
        string username,
        long companyId,
        string role,
        IEnumerable<string> permissions,
        long? branchId = null);
}

/// <summary>
/// التنفيذ الفعلي لخدمة الـ JWT.
/// يقرأ الإعدادات من IConfiguration (Jwt:Key, Jwt:Issuer, Jwt:Audience, Jwt:AccessTokenMinutes).
/// </summary>
public sealed class JwtService : IJwtService
{
    private readonly IConfiguration _config;

    public JwtService(IConfiguration config)
    {
        _config = config;
    }

    public string GenerateAccessToken(
        long userId,
        string username,
        long companyId,
        string role,
        IEnumerable<string> permissions,
        long? branchId = null)
    {
        // ===== قراءة الإعدادات مع Validation =====
        var key = _config["Jwt:Key"]
            ?? throw new InvalidOperationException("JWT Key is missing from configuration.");

        var issuer = _config["Jwt:Issuer"] ?? "KayanERP";
        var audience = _config["Jwt:Audience"] ?? "KayanERP.Clients";
        var minutes = int.TryParse(_config["Jwt:AccessTokenMinutes"], out var m) ? m : 60;

        // ===== بناء الـ Claims =====
        var claims = new List<Claim>
        {
            // Standard claims
            new(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new(JwtRegisteredClaimNames.UniqueName, username),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(JwtRegisteredClaimNames.Iat,
                DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(),
                ClaimValueTypes.Integer64),

            // Custom claims (يقرأها TenantContext)
            new("company_id", companyId.ToString()),
            new("role", role)
        };

        // Branch (اختياري)
        if (branchId.HasValue && branchId.Value > 0)
        {
            claims.Add(new Claim("branch_id", branchId.Value.ToString()));
        }

        // Permissions — كل واحدة كـ Claim منفصل
        foreach (var permission in permissions)
        {
            if (!string.IsNullOrWhiteSpace(permission))
            {
                claims.Add(new Claim("permission", permission));
            }
        }

        // ===== التوقيع =====
        var keyBytes = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
        var credentials = new SigningCredentials(keyBytes, SecurityAlgorithms.HmacSha256);

        // ===== إنشاء التوكن =====
        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: DateTime.UtcNow.AddMinutes(minutes),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}