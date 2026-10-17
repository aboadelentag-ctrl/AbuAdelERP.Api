using System;

namespace KayanERP.Shared.DTOs;

/// <summary>
/// DTO لعرض بيانات المورد.
/// </summary>
public class SupplierDto
{
    public long Id { get; set; }
    public long CompanyId { get; set; }
    public string SupplierCode { get; set; } = string.Empty;
    public string SupplierName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public string? TaxNumber { get; set; }
    public string? PaymentTerms { get; set; }
    public decimal? CreditLimit { get; set; }
    public bool Active { get; set; } = true;
    public string? Notes { get; set; }
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

/// <summary>
/// DTO لإنشاء أو تعديل مورد.
/// </summary>
public class SupplierCreateDto
{
    public string? SupplierCode { get; set; }
    public string SupplierName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public string? TaxNumber { get; set; }
    public string? PaymentTerms { get; set; } = "نقدي";
    public decimal? CreditLimit { get; set; }
    public bool Active { get; set; } = true;
    public string? Notes { get; set; }
}