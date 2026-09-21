using System;
using System.Collections.Generic;

namespace AbuAdelERP.Api.Models;

public partial class PurchaseOrder
{
    public long Id { get; set; }

    public long CompanyId { get; set; }

    public long SupplierId { get; set; }

    public long WarehouseId { get; set; }

    public string OrderNumber { get; set; } = null!;

    public DateOnly OrderDate { get; set; }

    public DateOnly? ExpectedDate { get; set; }

    public string Status { get; set; } = null!;

    public decimal Subtotal { get; set; }

    public decimal DiscountAmount { get; set; }

    public decimal TaxAmount { get; set; }

    public decimal TotalAmount { get; set; }

    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public long? CreatedBy { get; set; }

    public virtual Company Company { get; set; } = null!;

    public virtual AppUser? CreatedByNavigation { get; set; }

    public virtual ICollection<PurchaseOrderLine> PurchaseOrderLines { get; set; } = new List<PurchaseOrderLine>();

    public virtual Supplier Supplier { get; set; } = null!;

    public virtual Warehouse Warehouse { get; set; } = null!;
}
