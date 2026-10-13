using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KayanERP.Api.Models;

[Table("branch_transfers", Schema = "erp")]
public class BranchTransfer
{
    [Key]
    [Column("id")]
    public long Id { get; set; }

    [Column("company_id")]
    public long CompanyId { get; set; } = 1;

    [Required]
    [Column("transfer_number")]
    public string TransferNumber { get; set; } = null!;

    [Column("transfer_date")]
    public DateOnly TransferDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);

    [Column("source_warehouse_id")]
    public long SourceWarehouseId { get; set; }

    [Column("destination_warehouse_id")]
    public long DestinationWarehouseId { get; set; }

    [Column("destination_branch_id")]
    public long DestinationBranchId { get; set; }

    [Column("item_id")]
    public long ItemId { get; set; }

    [Column("batch_number")]
    public string? BatchNumber { get; set; }

    [Column("dispatched_qty")]
    public decimal DispatchedQty { get; set; }

    // أوعية النقل والتعبئة الميدانية
    [Column("container_type")]
    public string? ContainerType { get; set; } = "سرفيس"; // سرفيس، صاج/صينية، علبة/كرتونة

    [Column("inner_count")]
    public int InnerCount { get; set; } = 0; // عدد السرافيس أو الصواني

    [Column("crate_count")]
    public int CrateCount { get; set; } = 0; // عدد الأسبات أو الأقفاص الحاملة

    [Column("dispatched_count")]
    public int DispatchedCount { get; set; } = 1;

    [Column("driver_name")]
    public string? DriverName { get; set; }

    [Column("vehicle_number")]
    public string? VehicleNumber { get; set; }

    [Column("dispatcher_name")]
    public string? DispatcherName { get; set; }

    [Column("received_qty")]
    public decimal? ReceivedQty { get; set; }

    [Column("received_count")]
    public int? ReceivedCount { get; set; }

    [Column("received_inner_count")]
    public int? ReceivedInnerCount { get; set; }

    [Column("received_crate_count")]
    public int? ReceivedCrateCount { get; set; }

    [Column("receiver_name")]
    public string? ReceiverName { get; set; }

    [Column("status")]
    public string Status { get; set; } = "IN_TRANSIT"; // IN_TRANSIT, COMPLETED, IN_DISPUTE

    [Column("dispute_reason")]
    public string? DisputeReason { get; set; }

    [Column("dispute_charge_to")]
    public string? DisputeChargeTo { get; set; }

    [Column("dispute_notes")]
    public string? DisputeNotes { get; set; }

    [Column("resolved_by")]
    public string? ResolvedBy { get; set; }

    [Column("notes")]
    public string? Notes { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey(nameof(SourceWarehouseId))]
    public virtual Warehouse? SourceWarehouse { get; set; }

    [ForeignKey(nameof(DestinationWarehouseId))]
    public virtual Warehouse? DestinationWarehouse { get; set; }

    [ForeignKey(nameof(DestinationBranchId))]
    public virtual Branch? DestinationBranch { get; set; }

    [ForeignKey(nameof(ItemId))]
    public virtual Item? Item { get; set; }

    [ForeignKey(nameof(CompanyId))]
    public virtual Company? Company { get; set; }
}