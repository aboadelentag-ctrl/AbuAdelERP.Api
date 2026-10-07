using System;
using System.Collections.Generic;

namespace AbuAdelERP.UI.DTOs
{
    public class StockTransferLookupsDto
    {
        public List<TransferWarehouseLookupDto> Warehouses { get; set; } = new();
        public List<TransferItemLookupDto> Items { get; set; } = new();
    }

    public class TransferWarehouseLookupDto
    {
        public long Id { get; set; }
        public string WarehouseName { get; set; } = string.Empty;
    }

    public class TransferItemLookupDto
    {
        public long Id { get; set; }
        public string ItemCode { get; set; } = string.Empty;
        public string ItemName { get; set; } = string.Empty;
        public string Unit { get; set; } = string.Empty;
        public string ItemType { get; set; } = string.Empty;
    }

    public class StockTransferDetailDto
    {
        public long Id { get; set; }
        public string TransferNumber { get; set; } = string.Empty;
        public long FromWarehouseId { get; set; }
        public string FromWarehouseName { get; set; } = string.Empty;
        public long ToWarehouseId { get; set; }
        public string ToWarehouseName { get; set; } = string.Empty;
        public string Status { get; set; } = "IN_TRANSIT";
        public string? DriverName { get; set; }
        public string? VehicleNumber { get; set; }
        public string? Notes { get; set; }
        public string? CreatedBy { get; set; }
        public string? ReceivedBy { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? DispatchedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public List<StockTransferLineDetailDto> Lines { get; set; } = new();
    }

    public class StockTransferLineDetailDto
    {
        public long Id { get; set; }
        public long ItemId { get; set; }
        public string ItemCode { get; set; } = string.Empty;
        public string ItemName { get; set; } = string.Empty;
        public string Unit { get; set; } = string.Empty;
        public decimal DispatchedQty { get; set; }
        public decimal ReceivedQty { get; set; }
        public string? BatchNumber { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public decimal UnitCost { get; set; }
        public string LineStatus { get; set; } = "PENDING";
        public string? DiscrepancyReason { get; set; }
        public string? ResolutionNotes { get; set; }
        public decimal LossQty { get; set; }
    }

    public class CreateStockTransferDto
    {
        public long FromWarehouseId { get; set; }
        public long ToWarehouseId { get; set; }
        public string? DriverName { get; set; }
        public string? VehicleNumber { get; set; }
        public string? Notes { get; set; }
        public string? DispatchedBy { get; set; } = "أمين المخزن الراسل";
        public List<CreateStockTransferLineDto> Lines { get; set; } = new();
    }

    public class CreateStockTransferLineDto
    {
        public long ItemId { get; set; }
        public decimal DispatchedQty { get; set; }
        public string? BatchNumber { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public decimal UnitCost { get; set; }
        // حقل مساعد للعرض في الواجهة فقط
        public decimal AvailableStock { get; set; }
    }

    public class ConfirmTransferLineReceiveDto
    {
        public long LineId { get; set; }
        public decimal ReceivedQty { get; set; }
        public bool IsAccepted { get; set; } = true;
        public string? DiscrepancyReason { get; set; }
        public string? ReceivedBy { get; set; } = "أمين المخزن المستلم";
    }

    public class ResolveTransferDisputeDto
    {
        public long LineId { get; set; }
        public decimal FinalApprovedQty { get; set; }
        public decimal LossQty { get; set; }
        public string ResolutionNotes { get; set; } = string.Empty;
    }

    public class AvailableStockInfoDto
    {
        public decimal TotalAvailableQty { get; set; }
        public List<AvailableBatchItemDto> Batches { get; set; } = new();
    }

    public class AvailableBatchItemDto
    {
        public string BatchNumber { get; set; } = string.Empty;
        public DateTime? ExpiryDate { get; set; }
        public decimal AvailableQty { get; set; }
        public decimal UnitCost { get; set; }
    }
}