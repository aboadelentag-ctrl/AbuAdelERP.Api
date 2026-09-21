using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace AbuAdelERP.Api.Models;

[Table("purchase_invoices", Schema = "erp")]
public partial class PurchaseInvoice
{
    [Column("id")]
    public long Id { get; set; }

    [Column("invoice_number")]
    public string InvoiceNumber { get; set; } = null!;

    [Column("invoice_date")]
    public DateTime InvoiceDate { get; set; }

    [Column("supplier_id")]
    public long SupplierId { get; set; }

    [Column("total_amount")]
    public decimal TotalAmount { get; set; }

    public virtual ICollection<PurchaseInvoiceLine> InvoiceLines { get; set; } = new List<PurchaseInvoiceLine>();
}