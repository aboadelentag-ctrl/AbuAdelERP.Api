using KayanERP.Api.Models;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;

namespace KayanERP.Api.Services;

/// <summary>
/// واجهة خدمة إدارة Refresh Tokens.
/// </summary>
public interface IRefreshTokenService
{
    /// <summary>
    /// ينشئ Refresh Token جديد للمستخدم ويحفظه في الداتابيز.
    /// </summary>
    Task<RefreshToken> CreateAsync(
        long userId,
        string? ipAddress = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// يتحقق من صحة Refresh Token ويُرجع الـ Entity بتاعته.
    /// يرمي UnauthorizedAccessException لو التوكن مش صالح.
    /// </summary>
    Task<RefreshToken> ValidateAsync(
        string token,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// يلغي Refresh Token (للـ Logout أو الاستبدال).
    /// </summary>
    Task RevokeAsync(
        RefreshToken token,
        string reason,
        string? replacedByToken = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// يلغي كل Refresh Tokens الخاصة بمستخدم معيّن.
    /// </summary>
    Task RevokeAllForUserAsync(
        long userId,
        string reason,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// ينظّف التوكنات المنتهية من الداتابيز (Maintenance).
    /// </summary>
    Task<int> CleanupExpiredAsync(
        CancellationToken cancellationToken = default);
}

/// <summary>
/// التنفيذ الفعلي لخدمة Refresh Tokens.
/// </summary>
public sealed class RefreshTokenService : IRefreshTokenService
{
    private const int TokenSizeInBytes = 64;    // 64 bytes = 512 bits
    private const int RefreshTokenDays = 7;      // صلاحية 7 أيام

    private readonly AppDbContext _context;
    private readonly ILogger<RefreshTokenService> _logger;

    public RefreshTokenService(
        AppDbContext context,
        ILogger<RefreshTokenService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<RefreshToken> CreateAsync(
        long userId,
        string? ipAddress = null,
        CancellationToken cancellationToken = default)
    {
        // إنشاء توكن عشوائي آمن (cryptographically secure)
        var randomBytes = RandomNumberGenerator.GetBytes(TokenSizeInBytes);
        var tokenString = Convert.ToBase64String(randomBytes);

        var refreshToken = new RefreshToken
        {
            UserId = userId,
            Token = tokenString,
            ExpiresAt = DateTime.UtcNow.AddDays(RefreshTokenDays),
            CreatedAt = DateTime.UtcNow,
            CreatedByIp = ipAddress
        };

        _context.RefreshTokens.Add(refreshToken);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Refresh token created for user {UserId} (expires at {ExpiresAt})",
            userId, refreshToken.ExpiresAt);

        return refreshToken;
    }

    public async Task<RefreshToken> ValidateAsync(
        string token,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(token))
            throw new UnauthorizedAccessException("Refresh token is required.");

        var refreshToken = await _context.RefreshTokens
            .Include(rt => rt.User)
            .FirstOrDefaultAsync(rt => rt.Token == token, cancellationToken);

        if (refreshToken == null)
            throw new UnauthorizedAccessException("Refresh token not found.");

        if (refreshToken.IsRevoked)
            throw new UnauthorizedAccessException("Refresh token has been revoked.");

        if (refreshToken.IsExpired)
            throw new UnauthorizedAccessException("Refresh token has expired.");

        if (refreshToken.User == null || !refreshToken.User.IsActive)
            throw new UnauthorizedAccessException("User account is inactive.");

        return refreshToken;
    }

    public async Task RevokeAsync(
        RefreshToken token,
        string reason,
        string? replacedByToken = null,
        CancellationToken cancellationToken = default)
    {
        if (!token.IsActive) return;

        token.RevokedAt = DateTime.UtcNow;
        token.RevokedReason = reason;
        token.ReplacedByToken = replacedByToken;

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Refresh token revoked for user {UserId}. Reason: {Reason}",
            token.UserId, reason);
    }

    public async Task RevokeAllForUserAsync(
        long userId,
        string reason,
        CancellationToken cancellationToken = default)
    {
        var activeTokens = await _context.RefreshTokens
            .Where(rt => rt.UserId == userId && rt.RevokedAt == null)
            .ToListAsync(cancellationToken);

        var now = DateTime.UtcNow;
        foreach (var token in activeTokens)
        {
            token.RevokedAt = now;
            token.RevokedReason = reason;
        }

        if (activeTokens.Any())
        {
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "{Count} refresh tokens revoked for user {UserId}. Reason: {Reason}",
                activeTokens.Count, userId, reason);
        }
    }

    public async Task<int> CleanupExpiredAsync(
        CancellationToken cancellationToken = default)
    {
        var cutoffDate = DateTime.UtcNow.AddDays(-30); // نشيل اللي منتهي من 30 يوم

        var expiredTokens = await _context.RefreshTokens
            .Where(rt => rt.ExpiresAt < cutoffDate)
            .ToListAsync(cancellationToken);

        if (!expiredTokens.Any()) return 0;

        _context.RefreshTokens.RemoveRange(expiredTokens);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "{Count} expired refresh tokens cleaned up",
            expiredTokens.Count);

        return expiredTokens.Count;
    }
}