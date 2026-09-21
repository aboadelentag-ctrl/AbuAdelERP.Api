using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace AbuAdelERP.Api.Models;

public partial class StockBalance
{
    public long Id { get; set; }

    public long CompanyId { get; set; }

    public long WarehouseId { get; set; }

    public long ItemId { get; set; }

    public decimal Quantity { get; set; }

    public decimal ReservedQuantity { get; set; }

    public decimal? AvailableQuantity { get; set; }

    public DateTime LastUpdatedAt { get; set; }

    [JsonIgnore]
    public virtual Company Company { get; set; } = null!;

    [JsonIgnore]
    public virtual Item Item { get; set; } = null!;

    [JsonIgnore]
    public virtual Warehouse Warehouse { get; set; } = null!;
}