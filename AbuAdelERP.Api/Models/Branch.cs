using System;
using System.Collections.Generic;

namespace AbuAdelERP.Api.Models;

public partial class Branch
{
    public long Id { get; set; }

    public long CompanyId { get; set; }

    public string BranchName { get; set; } = null!;

    public string BranchCode { get; set; } = null!;

    public string? Phone { get; set; }

    public string? Address { get; set; }

    public bool Active { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual Company Company { get; set; } = null!;

    public virtual ICollection<SalesOrder> SalesOrders { get; set; } = new List<SalesOrder>();

    public virtual ICollection<Warehouse> Warehouses { get; set; } = new List<Warehouse>();
}
