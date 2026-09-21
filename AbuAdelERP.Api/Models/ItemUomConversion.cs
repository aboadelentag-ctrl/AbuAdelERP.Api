using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AbuAdelERP.Api.Models;

[Table("item_uom_conversions", Schema = "erp")]
public class ItemUomConversion
{
    [Key]
    [Column("id")]
    public long Id { get; set; }

    [Column("company_id")]
    public long CompanyId { get; set; } = 1;

    [Column("item_id")]
    public long? ItemId { get; set; } // Nullable: إذا كان null فهو تحويل عام، وإذا وجد فهو تحويل خاص بالصنف

    [ForeignKey("ItemId")]
    public virtual Item? Item { get; set; }

    [Column("from_unit")]
    public string FromUnit { get; set; } = string.Empty; // مثلاً: طن، شيكارة، بلكة

    [Column("to_unit")]
    public string ToUnit { get; set; } = string.Empty;     // مثلاً: كجم، قطعة

    [Column("conversion_factor")]
    public decimal ConversionFactor { get; set; }         // مثلاً: 1000 أو 48

    [Column("notes")]
    public string Notes { get; set; } = string.Empty;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}