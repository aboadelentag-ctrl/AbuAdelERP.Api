using System;
using System.Collections.Generic;

namespace KayanERP.Api.Models;

public partial class SalesOrder
{
    public long Id { get; set; }

    public long CompanyId { get; set; }

    public long? BranchId { get; set; }

    public long? CustomerId { get; set; }

    public long WarehouseId { get; set; }

    public string OrderNumber { get; set; } = null!;

    public DateOnly OrderDate { get; set; }

    public string Status { get; set; } = null!;

    public decimal Subtotal { get; set; }

    public decimal DiscountAmount { get; set; }

    public decimal TaxAmount { get; set; }

    public decimal TotalAmount { get; set; }

    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual Branch? Branch { get; set; }

    public virtual Company Company { get; set; } = null!;

    public virtual Customer? Customer { get; set; }

    public virtual ICollection<SalesOrderLine> SalesOrderLines { get; set; } = new List<SalesOrderLine>();

    public virtual Warehouse Warehouse { get; set; } = null!;
}
