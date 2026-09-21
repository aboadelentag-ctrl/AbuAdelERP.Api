using System.ComponentModel.DataAnnotations.Schema;

namespace AbuAdelERP.Api.Models;

[Table("purchase_invoice_lines", Schema = "erp")]
public partial class PurchaseInvoiceLine
{
    [Column("id")]
    public long Id { get; set; }

    [Column("purchase_invoice_id")]
    public long PurchaseInvoiceId { get; set; }

    [Column("item_id")]
    public long ItemId { get; set; }

    [Column("quantity")]
    public decimal Quantity { get; set; }

    [Column("unit_price")]
    public decimal UnitPrice { get; set; }

    [Column("line_total")]
    public decimal LineTotal { get; set; }

    [ForeignKey(nameof(PurchaseInvoiceId))]
    public virtual PurchaseInvoice PurchaseInvoice { get; set; } = null!;
}