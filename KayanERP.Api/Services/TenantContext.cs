using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace KayanERP.Api.Services;

/// <summary>
/// التنفيذ الفعلي لـ TenantContext.
/// يقرأ البيانات حصريًا من JWT Claims — لا يقبل أي قيمة من العميل مباشرة.
/// </summary>
public sealed class TenantContext : ITenantContext
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public TenantContext(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public long CompanyId
    {
        get
        {
            var user = _httpContextAccessor.HttpContext?.User;

            if (user?.Identity?.IsAuthenticated != true)
            {
                throw new UnauthorizedAccessException(
                    "No authenticated user found. Please login to continue.");
            }

            var claim = user.FindFirst("company_id")?.Value;

            if (string.IsNullOrEmpty(claim) ||
                !long.TryParse(claim, out var companyId) ||
                companyId <= 0)
            {
                throw new UnauthorizedAccessException(
                    "Company ID is missing or invalid in the authentication token.");
            }

            return companyId;
        }
    }

    public long? BranchId
    {
        get
        {
            var user = _httpContextAccessor.HttpContext?.User;
            if (user?.Identity?.IsAuthenticated != true) return null;

            var claim = user.FindFirst("branch_id")?.Value;

            return long.TryParse(claim, out var branchId) && branchId > 0
                ? branchId
                : null;
        }
    }

    public long? UserId
    {
        get
        {
            var user = _httpContextAccessor.HttpContext?.User;
            if (user?.Identity?.IsAuthenticated != true) return null;

            var claim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value
                     ?? user.FindFirst("sub")?.Value;

            return long.TryParse(claim, out var userId) && userId > 0
                ? userId
                : null;
        }
    }
}