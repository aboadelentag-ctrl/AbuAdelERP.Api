using System;
using System.Collections.Generic;

namespace KayanERP.Api.Models;

public partial class MaintenanceOrder
{
    public long Id { get; set; }

    public long CompanyId { get; set; }

    public long AssetId { get; set; }

    public string OrderNumber { get; set; } = null!;

    public string MaintenanceType { get; set; } = null!;

    public string Priority { get; set; } = null!;

    public string Status { get; set; } = null!;

    public DateOnly? ScheduledDate { get; set; }

    public DateOnly? CompletedDate { get; set; }

    public string Description { get; set; } = null!;

    public string? Technician { get; set; }

    public decimal Cost { get; set; }

    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual MaintenanceAsset Asset { get; set; } = null!;

    public virtual Company Company { get; set; } = null!;
}
