using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KayanERP.Api.Models;

[Table("sales_invoice_lines", Schema = "erp")]
public partial class SalesInvoiceLine
{
    [Key]
    [Column("id")]
    public long Id { get; set; }

    [Column("invoice_id")]
    public long InvoiceId { get; set; }

    [Column("item_id")]
    public long ItemId { get; set; }

    [Column("quantity")]
    public int Quantity { get; set; }

    [Column("unit_price")]
    public decimal UnitPrice { get; set; }

    [Column("line_total")]
    public decimal LineTotal { get; set; }

    [ForeignKey(nameof(InvoiceId))]
    public virtual SalesInvoice? Invoice { get; set; }

    [ForeignKey(nameof(ItemId))]
    public virtual Item? Item { get; set; }
}