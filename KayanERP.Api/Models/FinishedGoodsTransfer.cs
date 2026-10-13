using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KayanERP.Api.Models
{
    [Table("finished_goods_transfers", Schema = "erp")]
    public class FinishedGoodsTransfer
    {
        [Key]
        [Column("id")]
        public long Id { get; set; }

        [Column("transfer_number")]
        public string TransferNumber { get; set; } = string.Empty;

        [Column("transfer_date")]
        public DateOnly TransferDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);

        [Column("production_order_id")]
        public long? ProductionOrderId { get; set; }

        [Column("source_warehouse_id")]
        public long SourceWarehouseId { get; set; }

        [ForeignKey("SourceWarehouseId")]
        public virtual Warehouse? SourceWarehouse { get; set; }

        [Column("destination_warehouse_id")]
        public long DestinationWarehouseId { get; set; }

        [ForeignKey("DestinationWarehouseId")]
        public virtual Warehouse? DestinationWarehouse { get; set; }

        [Column("item_id")]
        public long ItemId { get; set; }

        [ForeignKey("ItemId")]
        public virtual Item? Item { get; set; }

        [Column("batch_number")]
        public string BatchNumber { get; set; } = string.Empty;

        [Column("expiry_date")]
        public DateOnly? ExpiryDate { get; set; }

        // 1. تسليم الإنتاج والصالة
        [Column("prod_dispatched_qty")]
        public decimal ProdDispatchedQty { get; set; }

        [Column("prod_dispatched_count")]
        public int ProdDispatchedCount { get; set; }

        [Column("prod_notes")]
        public string? ProdNotes { get; set; }

        [Column("prod_submitted_by")]
        public string? ProdSubmittedBy { get; set; } = "مشرف الصالة";

        // 2. تدقيق التكاليف
        [Column("costing_actual_qty")]
        public decimal? CostingActualQty { get; set; }

        [Column("costing_actual_count")]
        public int? CostingActualCount { get; set; }

        [Column("costing_weight_diff")]
        public decimal CostingWeightDiff { get; set; } = 0;

        [Column("costing_count_diff")]
        public int CostingCountDiff { get; set; } = 0;

        [Column("costing_discrepancy_flag")]
        public bool CostingDiscrepancyFlag { get; set; } = false;

        [Column("costing_auditor")]
        public string? CostingAuditor { get; set; }

        [Column("costing_audited_at")]
        public DateTime? CostingAuditedAt { get; set; }

        [Column("costing_notes")]
        public string? CostingNotes { get; set; }

        // نزاع بوابة التكاليف والإنتاج
        [Column("costing_dispute_status")]
        public string CostingDisputeStatus { get; set; } = "NONE";

        [Column("costing_dispute_reason")]
        public string? CostingDisputeReason { get; set; }

        [Column("costing_dispute_notes")]
        public string? CostingDisputeNotes { get; set; }

        [Column("costing_dispute_resolved_by")]
        public string? CostingDisputeResolvedBy { get; set; }

        [Column("costing_dispute_resolved_at")]
        public DateTime? CostingDisputeResolvedAt { get; set; }

        [Column("costing_dispute_charge_to")]
        public string? CostingDisputeChargeTo { get; set; }

        // 3. فحص الجودة
        [Column("qc_approved_qty")]
        public decimal QcApprovedQty { get; set; } = 0;

        [Column("qc_sample_qty")]
        public decimal QcSampleQty { get; set; } = 0;

        [Column("qc_rework_qty")]
        public decimal QcReworkQty { get; set; } = 0;

        [Column("qc_scrap_qty")]
        public decimal QcScrapQty { get; set; } = 0;

        [Column("qc_inspector")]
        public string? QcInspector { get; set; }

        [Column("qc_inspected_at")]
        public DateTime? QcInspectedAt { get; set; }

        [Column("qc_notes")]
        public string? QcNotes { get; set; }

        // 4. استلام مخزن الكنترول
        [Column("received_actual_qty")]
        public decimal? ReceivedActualQty { get; set; }

        [Column("received_actual_count")]
        public int? ReceivedActualCount { get; set; }

        [Column("control_receiver_name")]
        public string? ControlReceiverName { get; set; }

        [Column("control_received_at")]
        public DateTime? ControlReceivedAt { get; set; }

        [Column("control_notes")]
        public string? ControlNotes { get; set; }

        // نزاع بوابة الكنترول والجودة
        [Column("has_dispute")]
        public bool HasDispute { get; set; } = false;

        [Column("dispute_reason")]
        public string? DisputeReason { get; set; }

        [Column("dispute_status")]
        public string DisputeStatus { get; set; } = "NONE";

        [Column("dispute_opened_at")]
        public DateTime? DisputeOpenedAt { get; set; }

        [Column("dispute_resolved_by")]
        public string? DisputeResolvedBy { get; set; }

        [Column("dispute_resolution_notes")]
        public string? DisputeResolutionNotes { get; set; }

        [Column("dispute_resolved_at")]
        public DateTime? DisputeResolvedAt { get; set; }

        [Column("control_dispute_charge_to")]
        public string? ControlDisputeChargeTo { get; set; }

        [Column("status")]
        public string Status { get; set; } = "PENDING_COSTING";

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}