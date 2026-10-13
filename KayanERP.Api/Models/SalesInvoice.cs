using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace KayanERP.Api.Models;

[Table("sales_invoices", Schema = "erp")]
public partial class SalesInvoice
{
    [Column("id")]
    public long Id { get; set; }

    [Column("company_id")]
    public long CompanyId { get; set; } = 1;

    [Column("branch_id")]
    public long? BranchId { get; set; } = 1;

    [Column("invoice_number")]
    public string InvoiceNumber { get; set; } = string.Empty;

    [Column("invoice_date")]
    public DateTime InvoiceDate { get; set; }

    [Column("customer_id")]
    public long CustomerId { get; set; }

    [Column("total_amount")]
    public decimal TotalAmount { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public virtual ICollection<SalesInvoiceLine> InvoiceLines { get; set; } = new List<SalesInvoiceLine>();
}