using System;
using System.Collections.Generic;

namespace KayanERP.Shared.DTOs;

/// <summary>
/// ملخص مؤشرات المخزون.
/// </summary>
public class StockKpiSummaryDto
{
    public int TotalItemsCount { get; set; }
    public decimal TotalStockQuantity { get; set; }
    public decimal TotalAvailableQuantity { get; set; }
    public decimal EstimatedStockValue { get; set; }
    public int LowStockCount { get; set; }
    public int OutOfStockCount { get; set; }
}

/// <summary>
/// قوائم مرجعية لشاشة الأرصدة.
/// </summary>
public class StockLookupsDto
{
    public List<BranchForBalanceDto> Branches { get; set; } = new();
    public List<WarehouseForBalanceDto> Warehouses { get; set; } = new();
}

public class BranchForBalanceDto
{
    public long Id { get; set; }
    public string BranchCode { get; set; } = string.Empty;
    public string BranchName { get; set; } = string.Empty;
}

public class WarehouseForBalanceDto
{
    public long Id { get; set; }
    public long? BranchId { get; set; }
    public string WarehouseCode { get; set; } = string.Empty;
    public string WarehouseName { get; set; } = string.Empty;
}

/// <summary>
/// عرض رصيد صنف في مستودع محدد.
/// </summary>
public class StockBalanceListItemDto
{
    public long Id { get; set; }
    public long WarehouseId { get; set; }
    public string WarehouseCode { get; set; } = string.Empty;
    public string WarehouseName { get; set; } = string.Empty;
    public long? BranchId { get; set; }
    public string BranchName { get; set; } = string.Empty;
    public long ItemId { get; set; }
    public string ItemCode { get; set; } = string.Empty;
    public string ItemName { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal ReservedQuantity { get; set; }
    public decimal AvailableQuantity { get; set; }
    public decimal ReorderLevel { get; set; }
    public decimal SafetyStock { get; set; }
    public decimal BasePrice { get; set; }
    public decimal TotalStockValue { get; set; }
    public DateTime? LastUpdatedAt { get; set; }
    public string Status { get; set; } = "متوفر";
}