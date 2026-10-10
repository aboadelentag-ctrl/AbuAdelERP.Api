using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace AbuAdelERP.Api.Models;

[Table("stock_balances", Schema = "erp")]
public partial class StockBalance
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

    [Column("quantity")]
    public decimal Quantity { get; set; }

    [Column("reserved_quantity")]
    public decimal ReservedQuantity { get; set; }

    // محسوب آلياً داخل PostgreSQL (quantity - reserved_quantity)
    [Column("available_quantity")]
    [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
    public decimal? AvailableQuantity { get; set; }

    [Column("last_updated_at")]
    public DateTime LastUpdatedAt { get; set; }

    [ForeignKey(nameof(CompanyId))]
    [JsonIgnore]
    public virtual Company Company { get; set; } = null!;

    [ForeignKey(nameof(ItemId))]
    [JsonIgnore]
    public virtual Item Item { get; set; } = null!;

    [ForeignKey(nameof(WarehouseId))]
    [JsonIgnore]
    public virtual Warehouse Warehouse { get; set; } = null!;
}