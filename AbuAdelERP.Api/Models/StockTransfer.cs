using System;
using System.Collections.Generic;

namespace AbuAdelERP.Api.Models;

public partial class StockTransfer
{
    public long Id { get; set; }

    public long CompanyId { get; set; }

    public long FromWarehouseId { get; set; }

    public long ToWarehouseId { get; set; }

    public string TransferNumber { get; set; } = null!;

    public DateOnly TransferDate { get; set; }

    public string Status { get; set; } = null!;

    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual Company Company { get; set; } = null!;

    public virtual Warehouse FromWarehouse { get; set; } = null!;

    public virtual ICollection<StockTransferLine> StockTransferLines { get; set; } = new List<StockTransferLine>();

    public virtual Warehouse ToWarehouse { get; set; } = null!;
}
