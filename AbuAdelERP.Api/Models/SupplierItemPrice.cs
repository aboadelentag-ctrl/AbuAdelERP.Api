using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AbuAdelERP.Api.Models;

[Table("supplier_item_prices", Schema = "erp")]
public class SupplierItemPrice
{
    [Key]
    [Column("id")]
    public long Id { get; set; }

    [Column("supplier_id")]
    public long SupplierId { get; set; }

    [ForeignKey("SupplierId")]
    public virtual Supplier? Supplier { get; set; }

    [Column("item_id")]
    public long ItemId { get; set; }

    [ForeignKey("ItemId")]
    public virtual Item? Item { get; set; }

    [Column("price")]
    public decimal Price { get; set; }

    [Column("currency")]
    public string Currency { get; set; } = "EGP";

    [Column("lead_time_days")]
    public int LeadTimeDays { get; set; } = 1;

    [Column("effective_date")]
    public DateOnly EffectiveDate { get; set; } = DateOnly.FromDateTime(DateTime.UtcNow);

    [Column("is_preferred")]
    public bool IsPreferred { get; set; } = false;

    [Column("notes")]
    public string? Notes { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}