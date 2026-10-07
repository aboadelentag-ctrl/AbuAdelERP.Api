using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace AbuAdelERP.Api.DTOs
{
    // ==============================================================
    // 1. نماذج إنشاء وتدقيق التحويل
    // ==============================================================
    public class CreateTransferRequestDto
    {
        [Required(ErrorMessage = "يجب تحديد صالة التشغيل الراسلة.")]
        public long SourceWarehouseId { get; set; }

        [Required(ErrorMessage = "يجب تحديد مخزن الكنترول المستقبل.")]
        public long DestinationWarehouseId { get; set; }

        [Required(ErrorMessage = "يجب تحديد الصنف.")]
        public long ItemId { get; set; }

        [Required(ErrorMessage = "يجب تحديد رقم التشغيلة/اللوط.")]
        public string BatchNumber { get; set; } = string.Empty;

        public DateOnly? ExpiryDate { get; set; }

        [Range(0.001, double.MaxValue, ErrorMessage = "كمية التسليم يجب أن تكون أكبر من الصفر.")]
        public decimal DispatchedQty { get; set; }

        public int DispatchedCount { get; set; }
        public string? Notes { get; set; }

        [Required(ErrorMessage = "يجب تحديد اسم القائم بالتسليم.")]
        public string SubmittedBy { get; set; } = string.Empty;
        public string? SubmittedByCode { get; set; }
    }

    public class CostingAuditDto
    {
        [Required]
        public long TransferId { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "الوزن الفعلي لا يمكن أن يكون سالباً.")]
        public decimal ActualWeight { get; set; }

        public int ActualCount { get; set; }

        [Required(ErrorMessage = "يجب تحديد اسم مراجع التكاليف.")]
        public string AuditorName { get; set; } = string.Empty;

        public string? AuditorCode { get; set; }
        public string? Notes { get; set; }
        public bool EscalateDispute { get; set; } = false;
    }

    public class ResolveCostingDisputeDto
    {
        [Required]
        public long TransferId { get; set; }

        public decimal FinalAcceptedWeight { get; set; }
        public int FinalAcceptedCount { get; set; }

        [Required(ErrorMessage = "يجب تحديد اسم القائم بالاعتماد.")]
        public string ResolvedBy { get; set; } = string.Empty;

        public string? ResolvedByCode { get; set; }
        public string ChargeTo { get; set; } = string.Empty;

        [Required(ErrorMessage = "يجب توضيح أسباب وقرار التسوية.")]
        public string ResolutionNotes { get; set; } = string.Empty;
    }

    // ==============================================================
    // 2. نماذج توكيد الجودة (GMP / QC) وإعادة التشغيل
    // ==============================================================
    public class QcInspectionDto
    {
        [Required]
        public long TransferId { get; set; }

        public decimal ApprovedQty { get; set; }
        public decimal SampleQty { get; set; }
        public decimal ReworkQty { get; set; }
        public decimal ScrapQty { get; set; }

        [Required(ErrorMessage = "يجب تحديد اسم مفتش الجودة.")]
        public string InspectorName { get; set; } = string.Empty;

        public string? InspectorCode { get; set; }
        public string? Notes { get; set; }
    }

    public class ReSubmitReworkDto
    {
        [Required]
        public long TransferId { get; set; }

        [Range(0.001, double.MaxValue, ErrorMessage = "الكمية المعدلة يجب أن تكون أكبر من الصفر.")]
        public decimal AdjustedQty { get; set; }

        public int AdjustedCount { get; set; }

        [Required(ErrorMessage = "يجب تسجيل تفاصيل التعديل الفني.")]
        public string Notes { get; set; } = string.Empty;

        public string? ReworkBy { get; set; }
    }

    // ==============================================================
    // 3. نماذج استلام الكنترول وفض النزاع
    // ==============================================================
    public class ControlReceiptDto
    {
        [Required]
        public long TransferId { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "الكمية المستلمة لا يمكن أن تكون سالبة.")]
        public decimal ReceivedQty { get; set; }

        public int ReceivedCount { get; set; }

        [Required(ErrorMessage = "يجب تحديد اسم مستلم الكنترول.")]
        public string ReceiverName { get; set; } = string.Empty;

        public string? ReceiverCode { get; set; }
        public string? Notes { get; set; }
    }

    public class ResolveDisputeDto
    {
        [Required]
        public long TransferId { get; set; }

        public decimal FinalAcceptedQty { get; set; }

        [Required(ErrorMessage = "يجب تحديد اسم القائم بالاعتماد.")]
        public string ResolvedBy { get; set; } = string.Empty;

        public string? ResolvedByCode { get; set; }
        public string ChargeTo { get; set; } = string.Empty;

        [Required(ErrorMessage = "يجب توضيح محضر فض النزاع.")]
        public string ResolutionNotes { get; set; } = string.Empty;
    }

    // ==============================================================
    // 4. نماذج العرض والاستعلام (Views & Lookups)
    // ==============================================================
    public class FinishedGoodsTransferViewDto
    {
        public long Id { get; set; }
        public string TransferNumber { get; set; } = string.Empty;
        public DateOnly TransferDate { get; set; }
        public string SourceWarehouseName { get; set; } = string.Empty;
        public string DestinationWarehouseName { get; set; } = string.Empty;
        public string ItemName { get; set; } = string.Empty;
        public string ItemCode { get; set; } = string.Empty;
        public string BatchNumber { get; set; } = string.Empty;
        public DateOnly? ExpiryDate { get; set; }
        public decimal ProdDispatchedQty { get; set; }
        public int ProdDispatchedCount { get; set; }
        public string? ProdNotes { get; set; }
        public decimal? CostingActualQty { get; set; }
        public int? CostingActualCount { get; set; }
        public decimal CostingWeightDiff { get; set; }
        public int CostingCountDiff { get; set; }
        public bool CostingDiscrepancyFlag { get; set; }
        public string CostingDisputeStatus { get; set; } = string.Empty;
        public string? CostingDisputeReason { get; set; }
        public string? CostingDisputeChargeTo { get; set; }
        public decimal QcApprovedQty { get; set; }
        public decimal QcSampleQty { get; set; }
        public decimal QcReworkQty { get; set; }
        public decimal QcScrapQty { get; set; }
        public decimal? ReceivedActualQty { get; set; }
        public int? ReceivedActualCount { get; set; }
        public bool HasDispute { get; set; }
        public string? DisputeReason { get; set; }
        public string DisputeStatus { get; set; } = string.Empty;
        public string? ControlDisputeChargeTo { get; set; }
        public string Status { get; set; } = string.Empty;
    }

    public class WarehouseLookupDto
    {
        public long Id { get; set; }
        public string WarehouseName { get; set; } = string.Empty;
        public string? WarehouseCode { get; set; }
        public string? WarehouseType { get; set; }
    }

    public class FinishedItemLookupDto
    {
        public long Id { get; set; }
        public string ItemCode { get; set; } = string.Empty;
        public string ItemName { get; set; } = string.Empty;
        public string? ItemType { get; set; }
        public string? Unit { get; set; }
        public long? DepartmentId { get; set; }
        public int? ShelfLifeDays { get; set; }
    }

    public class TransferLookupsDto
    {
        public List<WarehouseLookupDto> Warehouses { get; set; } = new();
        public List<FinishedItemLookupDto> FinishedItems { get; set; } = new();
    }
}