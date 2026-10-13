using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace KayanERP.Api.Models;

[Table("crate_transactions", Schema = "erp")]
public partial class CrateTransaction
{
    [Column("id")]
    public long Id { get; set; }

    [Column("crate_id")]
    public long CrateId { get; set; }

    [Column("transaction_date")]
    public DateTime TransactionDate { get; set; }

    [Column("transaction_type")]
    public string TransactionType { get; set; } = null!; // DISPATCH (منصرف) | RETURN (مرتجع) | DAMAGE (هالك)

    [Column("party_type")]
    public string PartyType { get; set; } = null!; // BRANCH (فرع مبيعات) | CUSTOMER (عميل)

    [Column("party_id")]
    public long PartyId { get; set; } // رقم الفرع أو رقم العميل بناءً على نوع الطرف

    [Column("quantity")]
    public int Quantity { get; set; } // العدد

    [Column("notes")]
    public string? Notes { get; set; } // رقم إذن الصرف أو ملاحظات المستلم

    [ForeignKey(nameof(CrateId))]
    public virtual Crate Crate { get; set; } = null!;
}