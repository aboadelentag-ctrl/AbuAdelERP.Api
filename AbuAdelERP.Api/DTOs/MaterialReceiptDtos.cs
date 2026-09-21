using System;
using System.Collections.Generic;

namespace AbuAdelERP.Api.DTOs
{
    public class PurchaseOrderLookupDto
    {
        public long Id { get; set; }
        public string OrderNumber { get; set; } = string.Empty;
        public string SupplierName { get; set; } = string.Empty;
        public long WarehouseId { get; set; }
        public string WarehouseName { get; set; } = string.Empty;
        public string OrderDate { get; set; } = string.Empty;
        public List<PurchaseOrderLineDto> Lines { get; set; } = new();
    }

    public class PurchaseOrderLineDto
    {
        public long ItemId { get; set; }
        public string ItemCode { get; set; } = string.Empty;
        public string ItemName { get; set; } = string.Empty;
        public decimal OrderedQuantity { get; set; }
        public decimal UnitCost { get; set; }
        public bool HasExpiryDate { get; set; } = true;
        public int? ShelfLifeDays { get; set; }
    }

    public class MaterialReceiptDto
    {
        public long PurchaseOrderId { get; set; }
        public long WarehouseId { get; set; }
        public DateTime ReceiptDate { get; set; }

        // --- إضافات الجودة لتوثيق الشحنة الواردة ---
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

        // --- إضافات الجودة لكل بند صنف على حدة ---
        public string QcStatus { get; set; } = "Accepted"; // Accepted (مقبول), Partially_Accepted (قبول جزئي), Rejected (مرفوض)
        public decimal AcceptedQuantity { get; set; }
        public decimal RejectedQuantity { get; set; } = 0;
        public string? RejectionReason { get; set; }
    }
}