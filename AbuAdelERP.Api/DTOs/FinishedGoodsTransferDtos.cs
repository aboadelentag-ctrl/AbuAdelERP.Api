using System;

namespace AbuAdelERP.Api.DTOs
{
    public class CreateTransferRequestDto
    {
        public long SourceWarehouseId { get; set; }
        public long DestinationWarehouseId { get; set; }
        public long ItemId { get; set; }
        public string BatchNumber { get; set; } = string.Empty;
        public DateOnly? ExpiryDate { get; set; }
        public decimal DispatchedQty { get; set; }
        public int DispatchedCount { get; set; }
        public string? Notes { get; set; }
        public string SubmittedBy { get; set; } = "مشرف الصالة";
    }

    public class CostingAuditDto
    {
        public long TransferId { get; set; }
        public decimal ActualWeight { get; set; }
        public int ActualCount { get; set; }
        public string AuditorName { get; set; } = "مراجع التكاليف";
        public string? Notes { get; set; }
        public bool EscalateDispute { get; set; } = false;
    }

    public class ResolveCostingDisputeDto
    {
        public long TransferId { get; set; }
        public decimal FinalAcceptedWeight { get; set; }
        public int FinalAcceptedCount { get; set; }
        public string ResolvedBy { get; set; } = "لجنة الإنتاج والتكاليف";
        public string ChargeTo { get; set; } = string.Empty;
        public string ResolutionNotes { get; set; } = string.Empty;
    }

    public class QcInspectionDto
    {
        public long TransferId { get; set; }
        public decimal ApprovedQty { get; set; }
        public decimal SampleQty { get; set; }
        public decimal ReworkQty { get; set; }
        public decimal ScrapQty { get; set; }
        public string InspectorName { get; set; } = "مراقب الجودة";
        public string? Notes { get; set; }
    }

    public class ControlReceiptDto
    {
        public long TransferId { get; set; }
        public decimal ReceivedQty { get; set; }
        public int ReceivedCount { get; set; }
        public string ReceiverName { get; set; } = "أمين مخزن الكنترول";
        public string? Notes { get; set; }
    }

    public class ResolveDisputeDto
    {
        public long TransferId { get; set; }
        public decimal FinalAcceptedQty { get; set; }
        public string ResolvedBy { get; set; } = "لجنة فض المنازعات";
        public string ChargeTo { get; set; } = string.Empty;
        public string ResolutionNotes { get; set; } = string.Empty;
    }

    public class ReSubmitReworkDto
    {
        public long TransferId { get; set; }
        public decimal AdjustedQty { get; set; }
        public int AdjustedCount { get; set; }
        public string Notes { get; set; } = string.Empty;
    }

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
}