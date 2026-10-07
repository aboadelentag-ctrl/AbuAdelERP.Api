using System;
using System.Collections.Generic;

namespace AbuAdelERP.UI.DTOs
{
    public class StockCardLookupsResponse
    {
        public List<StockCardItemLookupDto> Items { get; set; } = new();
        public List<StockCardWarehouseLookupDto> Warehouses { get; set; } = new();
    }

    public class StockCardItemLookupDto
    {
        public long Id { get; set; }
        public string ItemCode { get; set; } = string.Empty;
        public string ItemName { get; set; } = string.Empty;
        public string Unit { get; set; } = string.Empty;
        public string ItemType { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
    }

    public class StockCardWarehouseLookupDto
    {
        public long Id { get; set; }
        public string WarehouseCode { get; set; } = string.Empty;
        public string WarehouseName { get; set; } = string.Empty;
    }

    public class StockCardReportDto
    {
        public long ItemId { get; set; }
        public string ItemCode { get; set; } = string.Empty;
        public string ItemName { get; set; } = string.Empty;
        public string Unit { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public decimal OpeningBalance { get; set; }
        public decimal TotalInbound { get; set; }
        public decimal TotalOutbound { get; set; }
        public decimal ClosingBalance { get; set; }
        public List<StockLedgerLineDto> Lines { get; set; } = new();
    }

    public class StockLedgerLineDto
    {
        public long MovementId { get; set; }
        public DateTime MovementDate { get; set; }
        public string MovementType { get; set; } = string.Empty;
        public string MovementTypeLabel { get; set; } = string.Empty;
        public string WarehouseName { get; set; } = string.Empty;
        public string BranchName { get; set; } = string.Empty;
        public decimal InQuantity { get; set; }
        public decimal OutQuantity { get; set; }
        public decimal BalanceAfter { get; set; }
        public decimal UnitCost { get; set; }
        public decimal TotalCost { get; set; }
        public string BatchNumber { get; set; } = string.Empty;
        public string ExpiryDate { get; set; } = string.Empty;
        public string ReferenceType { get; set; } = string.Empty;
        public long? ReferenceId { get; set; }
        public string? Notes { get; set; }
    }
}