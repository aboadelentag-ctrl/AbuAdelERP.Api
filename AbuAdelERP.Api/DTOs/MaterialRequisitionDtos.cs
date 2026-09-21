using System;
using System.Collections.Generic;

namespace AbuAdelERP.Api.DTOs
{
    public class CreateRequisitionDto
    {
        public long DepartmentId { get; set; }
        public long FromWarehouseId { get; set; }
        public string RequestedBy { get; set; } = "مشرف القسم";
        public string? Notes { get; set; }
        public List<CreateRequisitionLineDto> Lines { get; set; } = new();
    }

    public class CreateRequisitionLineDto
    {
        public long ItemId { get; set; }
        public decimal RequestedQty { get; set; }
    }

    public class DispatchRequisitionDto
    {
        public long RequisitionId { get; set; }
        public string DispatchedBy { get; set; } = "أمين المخزن";
        public List<DispatchLineDto> Lines { get; set; } = new();
    }

    public class DispatchLineDto
    {
        public long LineId { get; set; }
        public decimal DispatchedQty { get; set; }
        public string BatchNumber { get; set; } = string.Empty;
        public DateTime? ExpiryDate { get; set; }
        public decimal UnitCost { get; set; }
    }

    public class ReceiveLineItemDto
    {
        public long LineId { get; set; }
        public decimal ReceivedQty { get; set; }
        public bool IsAccepted { get; set; }
        public string? DiscrepancyReason { get; set; }
        public string ReceivedBy { get; set; } = "مشرف القسم";
    }

    public class ResolveDiscrepancyDto
    {
        public long LineId { get; set; }
        public decimal FinalApprovedQty { get; set; }
        public string WarehouseResolutionNotes { get; set; } = string.Empty;
    }

    public class RequisitionDetailDto
    {
        public long Id { get; set; }
        public string RequisitionNumber { get; set; } = string.Empty;
        public long DepartmentId { get; set; }
        public string DepartmentName { get; set; } = string.Empty;
        public long FromWarehouseId { get; set; }
        public string FromWarehouseName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string? Notes { get; set; }
        public string? RequestedBy { get; set; }
        public string? DispatchedBy { get; set; }
        public string? ReceivedBy { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? DispatchedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public List<RequisitionLineDetailDto> Lines { get; set; } = new();
    }

    public class RequisitionLineDetailDto
    {
        public long Id { get; set; }
        public long ItemId { get; set; }
        public string ItemCode { get; set; } = string.Empty;
        public string ItemName { get; set; } = string.Empty;
        public string Unit { get; set; } = string.Empty;
        public decimal RequestedQty { get; set; }
        public decimal DispatchedQty { get; set; }
        public decimal ReceivedQty { get; set; }
        public string? BatchNumber { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public decimal UnitCost { get; set; }
        public string LineStatus { get; set; } = string.Empty;
        public string? DiscrepancyReason { get; set; }
        public string? WarehouseNotes { get; set; }
    }

    public class FefoBatchSuggestionDto
    {
        public string BatchNumber { get; set; } = string.Empty;
        public DateTime? ExpiryDate { get; set; }
        public decimal AvailableQty { get; set; }
        public decimal UnitCost { get; set; }
    }
}