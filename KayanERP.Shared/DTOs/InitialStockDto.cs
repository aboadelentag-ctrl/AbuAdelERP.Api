using System;
using System.ComponentModel.DataAnnotations;

namespace KayanERP.Api.DTOs
{
    public class InitialStockDto
    {
        [Required(ErrorMessage = "يجب تحديد الصنف.")]
        public long ItemId { get; set; }

        [Required(ErrorMessage = "يجب تحديد المخزن.")]
        public long WarehouseId { get; set; }

        [Range(0.001, double.MaxValue, ErrorMessage = "يجب أن تكون الكمية أكبر من الصفر.")]
        public decimal Quantity { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "سعر الوحدة لا يمكن أن يكون سالباً.")]
        public decimal UnitCost { get; set; }

        public string? Notes { get; set; }
    }

    public class StockOperationResultDto
    {
        public bool Success { get; set; } = true;
        public string Message { get; set; } = string.Empty;
        public decimal CurrentBalance { get; set; }
    }

    public class StockBalanceViewDto
    {
        public long WarehouseId { get; set; }
        public string WarehouseName { get; set; } = string.Empty;
        public long ItemId { get; set; }
        public string ItemCode { get; set; } = string.Empty;
        public string ItemName { get; set; } = string.Empty;
        public string Unit { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public decimal AvailableQuantity { get; set; }
        public decimal ReservedQuantity { get; set; }
        public DateTime? LastUpdatedAt { get; set; }
    }

    public class StockMovementItemLedgerDto
    {
        public long MovementId { get; set; }
        public DateTime MovementDate { get; set; }
        public long WarehouseId { get; set; }
        public string WarehouseName { get; set; } = string.Empty;
        public string MovementType { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public decimal UnitCost { get; set; }
        public string ReferenceType { get; set; } = string.Empty;
        public long? ReferenceId { get; set; }
        public string Notes { get; set; } = string.Empty;
    }
}