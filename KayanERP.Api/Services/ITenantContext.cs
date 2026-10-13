namespace KayanERP.Api.Services;

/// <summary>
/// واجهة سياق المستأجر (Tenant Context).
/// تُستخدم في Global Query Filters لعزل بيانات كل شركة.
/// </summary>
public interface ITenantContext
{
    /// <summary>رقم الشركة الحالية — يُقرأ من JWT Claims.</summary>
    long CompanyId { get; }

    /// <summary>رقم الفرع الحالي (اختياري) — يُقرأ من JWT Claims.</summary>
    long? BranchId { get; }

    /// <summary>رقم المستخدم الحالي — يُقرأ من JWT Claims.</summary>
    long? UserId { get; }
}