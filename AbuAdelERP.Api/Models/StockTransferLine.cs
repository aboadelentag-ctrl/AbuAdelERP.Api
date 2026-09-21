using System;
using System.Collections.Generic;

namespace AbuAdelERP.Api.Models;

public partial class StockTransferLine
{
    public long Id { get; set; }

    public long TransferId { get; set; }

    public long ItemId { get; set; }

    public decimal Quantity { get; set; }

    public decimal ReceivedQuantity { get; set; }

    public virtual Item Item { get; set; } = null!;

    public virtual StockTransfer Transfer { get; set; } = null!;
}
