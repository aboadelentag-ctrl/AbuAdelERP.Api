using System;
using System.Collections.Generic;

namespace AbuAdelERP.Api.Models;

public partial class MaintenanceAsset
{
    public long Id { get; set; }

    public long CompanyId { get; set; }

    public long? DepartmentId { get; set; }

    public string AssetName { get; set; } = null!;

    public string AssetCode { get; set; } = null!;

    public string? AssetType { get; set; }

    public string? Location { get; set; }

    public bool Active { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual Company Company { get; set; } = null!;

    public virtual Department? Department { get; set; }

    public virtual ICollection<MaintenanceOrder> MaintenanceOrders { get; set; } = new List<MaintenanceOrder>();
}
