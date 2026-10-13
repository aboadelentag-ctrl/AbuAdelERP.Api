using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KayanERP.Api.Models
{
    [Table("stock_transfers", Schema = "erp")]
    public class StockTransfer
    {
        [Key]
        [Column("id")]
        public long Id { get; set; }

        [Column("company_id")]
        public long? CompanyId { get; set; } = 1;

        [ForeignKey("CompanyId")]
        public virtual Company? Company { get; set; }

        [Column("transfer_number")]
        public string TransferNumber { get; set; } = string.Empty;

        [Column("transfer_date")]
        public DateOnly? TransferDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);

        [Column("from_warehouse_id")]
        public long FromWarehouseId { get; set; }

        [ForeignKey("FromWarehouseId")]
        public virtual Warehouse? FromWarehouse { get; set; }

        [Column("to_warehouse_id")]
        public long ToWarehouseId { get; set; }

        [ForeignKey("ToWarehouseId")]
        public virtual Warehouse? ToWarehouse { get; set; }

        [Column("status")]
        public string Status { get; set; } = "IN_TRANSIT";

        [Column("driver_name")]
        public string? DriverName { get; set; }

        [Column("vehicle_number")]
        public string? VehicleNumber { get; set; }

        [Column("notes")]
        public string? Notes { get; set; }

        [Column("created_by")]
        public string? CreatedBy { get; set; }

        [Column("received_by")]
        public string? ReceivedBy { get; set; }

        [Column("created_at")]
        public DateTime? CreatedAt { get; set; } = DateTime.UtcNow;

        [Column("dispatched_at")]
        public DateTime? DispatchedAt { get; set; } = DateTime.UtcNow;

        [Column("completed_at")]
        public DateTime? CompletedAt { get; set; }

        [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; } = DateTime.UtcNow;

        public virtual ICollection<StockTransferLine> StockTransferLines { get; set; } = new List<StockTransferLine>();

        [NotMapped]
        public ICollection<StockTransferLine> Lines
        {
            get => StockTransferLines;
            set => StockTransferLines = value;
        }
    }
}