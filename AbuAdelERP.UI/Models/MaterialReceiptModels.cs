using System;
using System.Collections.Generic;

namespace AbuAdelERP.UI.Models
{
    public class MaterialReceiptDto
    {
        public long PurchaseOrderId { get; set; }
        public long WarehouseId { get; set; }
        public DateTime ReceiptDate { get; set; } = DateTime.Today;

        // حقول فحص الجودة
        public string QcInspectorName { get; set; } = string.Empty;
        public string? CoaNumber { get; set; }

        public List<ReceiptLineItemDto> Lines { get; set; } = new();
    }

    public class ReceiptLineItemDto
    {
        public long ItemId { get; set; }
        public string ItemCode { get; set; } = string.Empty;
        public string ItemName { get; set; } = string.Empty;
        public decimal OrderedQuantity { get; set; }
        public decimal ReceivedQuantity { get; set; }
        public string BatchNumber { get; set; } = string.Empty;
        public bool HasExpiryDate { get; set; } = true;
        public DateOnly? ExpiryDate { get; set; }

        // قرارات الجودة والكميات
        public string QcStatus { get; set; } = "Accepted"; // Accepted, Partially_Accepted, Rejected
        public decimal AcceptedQuantity { get; set; }
        public decimal RejectedQuantity { get; set; } = 0;
        public string? RejectionReason { get; set; }
    }
}