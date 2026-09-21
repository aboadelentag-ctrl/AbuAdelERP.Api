using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace AbuAdelERP.Api.Models;

[Table("sales_invoices", Schema = "erp")]
public partial class SalesInvoice
{
    [Column("id")]
    public long Id { get; set; }

    [Column("invoice_number")]
    public string InvoiceNumber { get; set; } = null!;

    [Column("invoice_date")]
    public DateTime InvoiceDate { get; set; }

    [Column("customer_id")]
    public long CustomerId { get; set; } // رقم العميل أو الفرع

    [Column("total_amount")]
    public decimal TotalAmount { get; set; } // إجمالي الفاتورة

    public virtual ICollection<SalesInvoiceLine> InvoiceLines { get; set; } = new List<SalesInvoiceLine>();
}