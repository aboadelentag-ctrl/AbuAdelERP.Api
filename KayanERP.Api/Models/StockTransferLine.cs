using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KayanERP.Api.Models
{
    [Table("stock_transfer_lines", Schema = "erp")]
    public class StockTransferLine
    {
        [Key]
        [Column("id")]
        public long Id { get; set; }

        [Column("transfer_id")]
        public long TransferId { get; set; }

        [ForeignKey("TransferId")]
        public virtual StockTransfer? Transfer { get; set; }

        [Column("item_id")]
        public long ItemId { get; set; }

        [ForeignKey("ItemId")]
        public virtual Item? Item { get; set; }

        [Column("quantity")]
        public decimal Quantity { get; set; } = 0;

        [NotMapped]
        public decimal DispatchedQty
        {
            get => Quantity;
            set => Quantity = value;
        }

        [Column("received_quantity")]
        public decimal ReceivedQuantity { get; set; } = 0;

        [NotMapped]
        public decimal ReceivedQty
        {
            get => ReceivedQuantity;
            set => ReceivedQuantity = value;
        }

        [Column("batch_number")]
        public string? BatchNumber { get; set; }

        [Column("expiry_date")]
        public DateTime? ExpiryDate { get; set; }

        [Column("unit_cost")]
        public decimal UnitCost { get; set; } = 0;

        [Column("line_status")]
        public string LineStatus { get; set; } = "PENDING";

        [Column("discrepancy_reason")]
        public string? DiscrepancyReason { get; set; }

        [Column("resolution_notes")]
        public string? ResolutionNotes { get; set; }

        [Column("loss_qty")]
        public decimal LossQty { get; set; } = 0;

        [Column("created_at")]
        public DateTime? CreatedAt { get; set; } = DateTime.UtcNow;
    }
}