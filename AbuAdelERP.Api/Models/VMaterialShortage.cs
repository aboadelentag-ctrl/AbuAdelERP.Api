using System;
using System.Collections.Generic;

namespace AbuAdelERP.Api.Models;

public partial class VMaterialShortage
{
    public long? CompanyId { get; set; }

    public long? ItemId { get; set; }

    public string? ItemCode { get; set; }

    public string? ItemName { get; set; }

    public decimal? CurrentStock { get; set; }

    public decimal? MinimumStock { get; set; }

    public decimal? RequiredPurchasing { get; set; }
}
