using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace KayanERP.Api.Models
{
    [Table("purchase_order_lines", Schema = "erp")]
    public partial class PurchaseOrderLine
    {
        [Key]
        [Column("id")]
        public long Id { get; set; }

        [Column("purchase_order_id")]
        public long PurchaseOrderId { get; set; }

        [Column("item_id")]
        public long ItemId { get; set; }

        [Column("quantity")]
        public decimal Quantity { get; set; }

        [Column("unit_price")]
        public decimal UnitPrice { get; set; } = 0;

        // الحقول المطلوبة لخدمات ProcurementService و PurchasingService
        [Column("received_quantity")]
        public decimal ReceivedQuantity { get; set; } = 0;

        [Column("discount_amount")]
        public decimal DiscountAmount { get; set; } = 0;

        [Column("tax_amount")]
        public decimal TaxAmount { get; set; } = 0;

        [Column("line_total")]
        public decimal LineTotal { get; set; } = 0;

        // خانة الملاحظات الخاصة بالصنف
        [Column("notes")]
        public string? Notes { get; set; }

        [ForeignKey("PurchaseOrderId")]
        [JsonIgnore]
        public virtual PurchaseOrder? PurchaseOrder { get; set; }

        [ForeignKey("ItemId")]
        [JsonIgnore]
        public virtual Item? Item { get; set; }
    }
}