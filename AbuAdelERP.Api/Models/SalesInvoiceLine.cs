using System.ComponentModel.DataAnnotations.Schema;

namespace AbuAdelERP.Api.Models;

[Table("sales_invoice_lines", Schema = "erp")]
public partial class SalesInvoiceLine
{
    [Column("id")]
    public long Id { get; set; }

    [Column("sales_invoice_id")]
    public long SalesInvoiceId { get; set; }

    [Column("item_id")]
    public long ItemId { get; set; } // رقم المنتج التام المباع (مثل: تورتة، جاتوه)

    [Column("quantity")]
    public int Quantity { get; set; } // الكمية

    [Column("unit_price")]
    public decimal UnitPrice { get; set; } // سعر الوحدة

    [Column("line_total")]
    public decimal LineTotal { get; set; } // إجمالي السطر (الكمية × السعر)

    [ForeignKey(nameof(SalesInvoiceId))]
    public virtual SalesInvoice SalesInvoice { get; set; } = null!;
}