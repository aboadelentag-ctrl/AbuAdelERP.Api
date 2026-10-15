namespace KayanERP.Api.Models.Interfaces;

/// <summary>
/// Marker Interface للـ Entities اللي بتخص شركة معينة (Tenant).
/// أي entity عنده CompanyId لازم يطبق الـ interface ده.
/// 
/// AppDbContext بيستخدم الـ interface ده عشان يضيف Global Query Filter 
/// تلقائيًا لكل entity بيطبقه — يعني كل الجداول معزولة تلقائيًا.
/// </summary>
public interface ITenantEntity
{
    /// <summary>
    /// رقم الشركة اللي البيانات دي بتخصها.
    /// </summary>
    long CompanyId { get; set; }
}