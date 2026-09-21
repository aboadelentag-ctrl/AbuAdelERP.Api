using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace AbuAdelERP.Api.Models;

public partial class StockMovement
{
    public long Id { get; set; }

    public long CompanyId { get; set; }

    public long WarehouseId { get; set; }

    public long ItemId { get; set; }

    public string MovementType { get; set; } = null!;

    public decimal Quantity { get; set; }

    public decimal UnitCost { get; set; }

    public string? ReferenceType { get; set; }

    public long? ReferenceId { get; set; }

    public string? Notes { get; set; }

    public DateTime MovementAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public long? CreatedBy { get; set; }

    [JsonIgnore]
    public virtual Company Company { get; set; } = null!;

    [JsonIgnore]
    public virtual AppUser? CreatedByNavigation { get; set; }

    [JsonIgnore]
    public virtual Item Item { get; set; } = null!;

    [JsonIgnore]
    public virtual Warehouse Warehouse { get; set; } = null!;
}