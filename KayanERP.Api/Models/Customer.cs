using System;
using System.Collections.Generic;

namespace KayanERP.Api.Models;

public partial class Customer
{
    public long Id { get; set; }

    public long CompanyId { get; set; }

    public string CustomerName { get; set; } = null!;

    public string CustomerCode { get; set; } = null!;

    public string CustomerType { get; set; } = null!;

    public string? Phone { get; set; }

    public string? Email { get; set; }

    public string? Address { get; set; }

    public string? TaxNumber { get; set; }

    public string? PaymentTerms { get; set; }

    public decimal CreditLimit { get; set; }

    public bool Active { get; set; }

    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual Company Company { get; set; } = null!;

    public virtual ICollection<SalesOrder> SalesOrders { get; set; } = new List<SalesOrder>();
}
