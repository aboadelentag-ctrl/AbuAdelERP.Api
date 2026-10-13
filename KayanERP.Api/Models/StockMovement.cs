using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace KayanERP.Api.Models;

[Table("stock_movements", Schema = "erp")]
public partial class StockMovement
{
    [Key]
    [Column("id")]
    public long Id { get; set; }

    [Column("company_id")]
    public long CompanyId { get; set; }

    [Column("warehouse_id")]
    public long WarehouseId { get; set; }

    [Column("item_id")]
    public long ItemId { get; set; }

    [Column("movement_type")]
    public string MovementType { get; set; } = null!;

    [Column("quantity")]
    public decimal Quantity { get; set; }

    [Column("unit_cost")]
    public decimal UnitCost { get; set; }

    [Column("reference_type")]
    public string? ReferenceType { get; set; }

    [Column("reference_id")]
    public long? ReferenceId { get; set; }

    [Column("notes")]
    public string? Notes { get; set; }

    [Column("movement_at")]
    public DateTime MovementAt { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("created_by")]
    public long? CreatedBy { get; set; }

    [ForeignKey(nameof(CompanyId))]
    [JsonIgnore]
    public virtual Company Company { get; set; } = null!;

    // ربط صريح مع عمود created_by لمنع توليد created_by_navigation_id الوهمي
    [ForeignKey(nameof(CreatedBy))]
    [JsonIgnore]
    public virtual AppUser? CreatedByNavigation { get; set; }

    [ForeignKey(nameof(ItemId))]
    [JsonIgnore]
    public virtual Item Item { get; set; } = null!;

    [ForeignKey(nameof(WarehouseId))]
    [JsonIgnore]
    public virtual Warehouse Warehouse { get; set; } = null!;
}