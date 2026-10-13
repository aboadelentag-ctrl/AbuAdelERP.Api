using System;
using System.Collections.Generic;

namespace KayanERP.Api.Models;

public partial class VCoreItem
{
    public long? Id { get; set; }

    public long? CompanyId { get; set; }

    public long? CategoryId { get; set; }

    public string? CategoryName { get; set; }

    public string? CategoryCode { get; set; }

    public string? ItemName { get; set; }

    public string? ItemCode { get; set; }

    public string? ItemType { get; set; }

    public string? Barcode { get; set; }

    public string? Unit { get; set; }

    public decimal? PurchasePrice { get; set; }

    public decimal? CostPrice { get; set; }

    public decimal? SellingPrice { get; set; }

    public decimal? MinimumStock { get; set; }

    public decimal? MaximumStock { get; set; }

    public bool? Active { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }
}
