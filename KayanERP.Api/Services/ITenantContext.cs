using Microsoft.AspNetCore.Http;

namespace KayanERP.Api.Services;

public interface ITenantContext
{
    long CompanyId { get; }
    long? BranchId { get; }
}

public sealed class TenantContext : ITenantContext
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public TenantContext(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    /// <summary>
    /// جلب رقم الشركة المستأجرة من الـ Header (X-Tenant-Id)
    /// وفي حال عدم إرساله، يتم افتراض المستأجر رقم 1 (حلواني أبو عادل) لضمان استقرار التشغيل الحالي
    /// </summary>
    public long CompanyId
    {
        get
        {
            var context = _httpContextAccessor.HttpContext;
            if (context != null &&
                context.Request.Headers.TryGetValue("X-Tenant-Id", out var tenantVal) &&
                long.TryParse(tenantVal, out var tenantId) && tenantId > 0)
            {
                return tenantId;
            }

            return 1; // الافتراضي: المستأجر الأول
        }
    }

    /// <summary>
    /// جلب رقم الفرع المختار من الـ Header (X-Branch-Id)
    /// </summary>
    public long? BranchId
    {
        get
        {
            var context = _httpContextAccessor.HttpContext;
            if (context != null &&
                context.Request.Headers.TryGetValue("X-Branch-Id", out var branchVal) &&
                long.TryParse(branchVal, out var branchId) && branchId > 0)
            {
                return branchId;
            }

            return null;
        }
    }
}