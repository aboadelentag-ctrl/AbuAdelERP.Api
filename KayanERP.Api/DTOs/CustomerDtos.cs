namespace KayanERP.Api.DTOs;

public class CustomerItemDto
{
    public long Id { get; set; }
    public long CompanyId { get; set; }
    public string CustomerCode { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerType { get; set; } = "RETAIL";
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public decimal CreditLimit { get; set; }
    public string? PaymentTerms { get; set; }
    public string? TaxNumber { get; set; }
    public bool Active { get; set; } = true;
    public string? Notes { get; set; }
}

public class CreateCustomerDto
{
    public string CustomerCode { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerType { get; set; } = "RETAIL";
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public decimal CreditLimit { get; set; }
    public string? PaymentTerms { get; set; }
    public string? TaxNumber { get; set; }
    public bool Active { get; set; } = true;
    public string? Notes { get; set; }
}

public class UpdateCustomerDto
{
    public string CustomerCode { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerType { get; set; } = "RETAIL";
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public decimal CreditLimit { get; set; }
    public string? PaymentTerms { get; set; }
    public string? TaxNumber { get; set; }
    public bool Active { get; set; } = true;
    public string? Notes { get; set; }
}