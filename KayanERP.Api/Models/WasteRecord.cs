using System;
using System.Collections.Generic;

namespace KayanERP.Api.Models;

public partial class WasteRecord
{
    public long Id { get; set; }

    public long CompanyId { get; set; }

    public long ItemId { get; set; }

    public long? WarehouseId { get; set; }

    public long? ProductionOrderId { get; set; }

    public long? ProductionStageId { get; set; }

    public decimal Quantity { get; set; }

    public string WasteReason { get; set; } = null!;

    public DateTime WasteDate { get; set; }

    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; }

    public long? CreatedBy { get; set; }

    public virtual Company Company { get; set; } = null!;

    public virtual AppUser? CreatedByNavigation { get; set; }

    public virtual Item Item { get; set; } = null!;

    public virtual ProductionOrder? ProductionOrder { get; set; }

    public virtual ProductionOrderStage? ProductionStage { get; set; }

    public virtual Warehouse? Warehouse { get; set; }
}
