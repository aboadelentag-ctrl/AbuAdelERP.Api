using System;
using System.Collections.Generic;

namespace AbuAdelERP.Api.Models;

public partial class PurchaseOrderLine
{
    public long Id { get; set; }

    public long PurchaseOrderId { get; set; }

    public long ItemId { get; set; }

    public decimal Quantity { get; set; }

    public decimal UnitPrice { get; set; }

    public decimal DiscountAmount { get; set; }

    public decimal TaxAmount { get; set; }

    public decimal? LineTotal { get; set; }

    public decimal ReceivedQuantity { get; set; }

    public string? Notes { get; set; }

    public virtual Item Item { get; set; } = null!;

    public virtual PurchaseOrder PurchaseOrder { get; set; } = null!;
}
