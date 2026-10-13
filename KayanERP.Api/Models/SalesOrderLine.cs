using System;
using System.Collections.Generic;

namespace KayanERP.Api.Models;

public partial class SalesOrderLine
{
    public long Id { get; set; }

    public long SalesOrderId { get; set; }

    public long ItemId { get; set; }

    public decimal Quantity { get; set; }

    public decimal UnitPrice { get; set; }

    public decimal DiscountAmount { get; set; }

    public decimal TaxAmount { get; set; }

    public decimal? LineTotal { get; set; }

    public decimal DeliveredQuantity { get; set; }

    public string? Notes { get; set; }

    public virtual Item Item { get; set; } = null!;

    public virtual SalesOrder SalesOrder { get; set; } = null!;
}
