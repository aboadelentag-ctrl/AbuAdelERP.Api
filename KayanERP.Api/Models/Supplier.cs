using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace KayanERP.Api.Models;

[Table("suppliers", Schema = "erp")]
public partial class Supplier
{
    public long Id { get; set; }

    public long CompanyId { get; set; }

    public string SupplierName { get; set; } = null!;

    public string SupplierCode { get; set; } = null!;

    public string? Phone { get; set; }

    public string? Email { get; set; }

    public string? Address { get; set; }

    public string? TaxNumber { get; set; }

    public string? PaymentTerms { get; set; }

    public decimal CreditLimit { get; set; }

    public bool Active { get; set; } = true;

    [Column("is_approved")]
    public bool IsApproved { get; set; } = false;

    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public virtual Company Company { get; set; } = null!;

    public virtual ICollection<PurchaseOrder> PurchaseOrders { get; set; } = new List<PurchaseOrder>();
}