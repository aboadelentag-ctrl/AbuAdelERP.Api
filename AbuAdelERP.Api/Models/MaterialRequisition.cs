using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AbuAdelERP.Api.Models
{
    [Table("material_requisitions", Schema = "erp")]
    public class MaterialRequisition
    {
        [Key]
        [Column("id")]
        public long Id { get; set; }

        [Column("requisition_number")]
        public string RequisitionNumber { get; set; } = string.Empty;

        [Column("department_id")]
        public long DepartmentId { get; set; }

        [ForeignKey("DepartmentId")]
        public virtual Department? Department { get; set; }

        [Column("from_warehouse_id")]
        public long FromWarehouseId { get; set; }

        [ForeignKey("FromWarehouseId")]
        public virtual Warehouse? FromWarehouse { get; set; }

        [Column("to_warehouse_id")]
        public long? ToWarehouseId { get; set; }

        [ForeignKey("ToWarehouseId")]
        public virtual Warehouse? ToWarehouse { get; set; }

        [Column("status")]
        public string Status { get; set; } = "SUBMITTED";

        [Column("notes")]
        public string? Notes { get; set; }

        [Column("requested_by")]
        public string? RequestedBy { get; set; }

        [Column("dispatched_by")]
        public string? DispatchedBy { get; set; }

        [Column("received_by")]
        public string? ReceivedBy { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column("dispatched_at")]
        public DateTime? DispatchedAt { get; set; }

        [Column("completed_at")]
        public DateTime? CompletedAt { get; set; }

        public virtual ICollection<MaterialRequisitionLine> Lines { get; set; } = new List<MaterialRequisitionLine>();
    }

    [Table("material_requisition_lines", Schema = "erp")]
    public class MaterialRequisitionLine
    {
        [Key]
        [Column("id")]
        public long Id { get; set; }

        [Column("requisition_id")]
        public long RequisitionId { get; set; }

        [ForeignKey("RequisitionId")]
        public virtual MaterialRequisition? Requisition { get; set; }

        [Column("item_id")]
        public long ItemId { get; set; }

        [ForeignKey("ItemId")]
        public virtual Item? Item { get; set; }

        [Column("requested_qty")]
        public decimal RequestedQty { get; set; }

        [Column("dispatched_qty")]
        public decimal DispatchedQty { get; set; } = 0;

        [Column("received_qty")]
        public decimal ReceivedQty { get; set; } = 0;

        [Column("batch_number")]
        public string? BatchNumber { get; set; }

        [Column("expiry_date")]
        public DateTime? ExpiryDate { get; set; }

        [Column("unit_cost")]
        public decimal UnitCost { get; set; } = 0;

        [Column("line_status")]
        public string LineStatus { get; set; } = "PENDING"; // PENDING, DISPATCHED, ACCEPTED, DISPUTED

        [Column("discrepancy_reason")]
        public string? DiscrepancyReason { get; set; }

        [Column("warehouse_notes")]
        public string? WarehouseNotes { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}