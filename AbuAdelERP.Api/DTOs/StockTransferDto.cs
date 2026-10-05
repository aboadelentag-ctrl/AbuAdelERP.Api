using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace AbuAdelERP.Api.DTOs
{
    // ==============================================================
    // 1. القوائم المرجعية الموحدة لشاشات التحويل (Lookups)
    // ==============================================================
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
        public string? Unit { get; set; }
        public string? ItemType { get; set; }
    }

    public class StockTransferLookupsDto
    {
        public List<TransferWarehouseLookupDto> Warehouses { get; set; } = new();
        public List<TransferItemLookupDto> Items { get; set; } = new();
    }

    // ==============================================================
    // 2. نموذج إنشاء وصرف إذن التحويل (Dispatch)
    // ==============================================================
    public class CreateStockTransferDto
    {
        [Required(ErrorMessage = "يجب تحديد المخزن الراسل.")]
        [Range(1, long.MaxValue, ErrorMessage = "معرف المخزن الراسل غير صالح.")]
        public long FromWarehouseId { get; set; }

        [Required(ErrorMessage = "يجب تحديد المخزن المستقبل.")]
        [Range(1, long.MaxValue, ErrorMessage = "معرف المخزن المستقبل غير صالح.")]
        public long ToWarehouseId { get; set; }

        public string? DriverName { get; set; }
        public string? VehicleNumber { get; set; }
        public string? Notes { get; set; }

        [Required(ErrorMessage = "يجب تحديد اسم القائم بالصرف.")]
        public string DispatchedBy { get; set; } = "أمين المخزن الراسل";

        [Required(ErrorMessage = "يجب إدراج صنف واحد على الأقل في إذن التحويل.")]
        public List<CreateStockTransferLineDto> Lines { get; set; } = new();
    }

    public class CreateStockTransferLineDto
    {
        [Required(ErrorMessage = "يجب تحديد الصنف.")]
        [Range(1, long.MaxValue, ErrorMessage = "معرف الصنف غير صالح.")]
        public long ItemId { get; set; }

        [Required(ErrorMessage = "يجب تحديد كمية الصرف.")]
        [Range(0.001, double.MaxValue, ErrorMessage = "كمية الصرف يجب أن تكون أكبر من الصفر.")]
        public decimal DispatchedQty { get; set; }

        public string BatchNumber { get; set; } = "LOT-DEFAULT";
        public DateTime? ExpiryDate { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "تكلفة الوحدة لا يمكن أن تكون سالبة.")]
        public decimal UnitCost { get; set; } = 0;
    }

    // ==============================================================
    // 3. نموذج تأكيد استلام سطر التحويل بالمخزن المستقبل (Receive)
    // ==============================================================
    public class ConfirmTransferLineReceiveDto
    {
        [Required(ErrorMessage = "يجب تحديد سطر التحويل.")]
        [Range(1, long.MaxValue, ErrorMessage = "معرف السطر غير صالح.")]
        public long LineId { get; set; }

        [Required(ErrorMessage = "يجب إدخال الكمية المستلمة.")]
        [Range(0, double.MaxValue, ErrorMessage = "الكمية المستلمة لا يمكن أن تكون سالبة.")]
        public decimal ReceivedQty { get; set; }

        public bool IsAccepted { get; set; }
        public string? DiscrepancyReason { get; set; }

        [Required(ErrorMessage = "يجب تحديد اسم مستلم البضاعة.")]
        public string ReceivedBy { get; set; } = "أمين المخزن المستقبل";
    }

    // ==============================================================
    // 4. نموذج فض النزاع وتسوية الفروق والعجز (Dispute Resolution)
    // ==============================================================
    public class ResolveTransferDisputeDto
    {
        [Required(ErrorMessage = "يجب تحديد سطر التحويل المتنازع عليه.")]
        [Range(1, long.MaxValue, ErrorMessage = "معرف السطر غير صالح.")]
        public long LineId { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "الكمية المعتمدة لا يمكن أن تكون سالبة.")]
        public decimal FinalApprovedQty { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "كمية العجز/الهالك لا يمكن أن تكون سالبة.")]
        public decimal LossQty { get; set; }

        [Required(ErrorMessage = "يجب كتابة مبررات وقرار لجنة فض النزاع.")]
        public string ResolutionNotes { get; set; } = string.Empty;

        public string ResolvedBy { get; set; } = "لجنة فض المنازعات";
    }

    // ==============================================================
    // 5. تفاصيل إذن التحويل وسطور الحركات (Detailed View)
    // ==============================================================
    public class StockTransferDetailDto
    {
        public long Id { get; set; }
        public string TransferNumber { get; set; } = string.Empty;
        public long FromWarehouseId { get; set; }
        public string FromWarehouseName { get; set; } = string.Empty;
        public long ToWarehouseId { get; set; }
        public string ToWarehouseName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
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
        public string LineStatus { get; set; } = string.Empty;
        public string? DiscrepancyReason { get; set; }
        public string? ResolutionNotes { get; set; }
        public decimal LossQty { get; set; }
    }

    // ==============================================================
    // 6. استعلام الرصيد اللحظي واللوطات المتاحة للصرف
    // ==============================================================
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