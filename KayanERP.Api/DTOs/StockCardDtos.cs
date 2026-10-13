using System;
using System.Collections.Generic;

namespace KayanERP.Api.DTOs
{
    public class ItemStockSummaryDto
    {
        public long ItemId { get; set; }
        public string ItemCode { get; set; } = string.Empty;
        public string ItemName { get; set; } = string.Empty;
        public string Unit { get; set; } = string.Empty;
        public long WarehouseId { get; set; }
        public string WarehouseName { get; set; } = string.Empty;
        public decimal TotalQuantity { get; set; }
        public decimal AvailableQuantity { get; set; }
        public decimal ReservedQuantity { get; set; }
        public bool HasExpiryDate { get; set; }

        public decimal ReorderLevel { get; set; }
        public decimal SafetyStock { get; set; }
        public decimal MaxStock { get; set; }
        public string StockStatus { get; set; } = "Safe"; // Safe, ReorderNeeded, Critical
        public string StatusMessage { get; set; } = string.Empty;
    }

    public class ItemBatchStockDto
    {
        public string BatchNumber { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public string? ExpiryDate { get; set; }
        public int? DaysRemaining { get; set; }
        public string Status { get; set; } = "Valid";
    }

    public class StockMovementLedgerDto
    {
        public long MovementId { get; set; }
        public DateTime MovementAt { get; set; }
        public string MovementType { get; set; } = string.Empty;
        public decimal InQuantity { get; set; }
        public decimal OutQuantity { get; set; }
        public decimal RunningBalance { get; set; }
        public decimal UnitCost { get; set; }
        public string ReferenceType { get; set; } = string.Empty;
        public long? ReferenceId { get; set; }
        public string Notes { get; set; } = string.Empty;
    }

    public class ItemStockCardDetailDto
    {
        public ItemStockSummaryDto Summary { get; set; } = new();
        public List<ItemBatchStockDto> Batches { get; set; } = new();
        public List<StockMovementLedgerDto> Ledger { get; set; } = new();
    }
}