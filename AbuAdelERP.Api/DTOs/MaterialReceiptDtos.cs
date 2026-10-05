using System;
using System.Collections.Generic;

namespace AbuAdelERP.Api.DTOs
{
    // ========================================================
    // 1. DTOs فحص الجودة واستلام الخامات الميداني (GMP Inbound)
    // ========================================================
    public class MaterialReceiptPostDto
    {
        public long SupplierId { get; set; }
        public long InvoiceId { get; set; }
        public long WarehouseId { get; set; }
        public string InspectorName { get; set; } = string.Empty;
        public string InspectorCode { get; set; } = string.Empty;
        public string CoaNumber { get; set; } = string.Empty;
        public List<ReceiptItemInputDto> Items { get; set; } = new();
    }

    public class ReceiptItemInputDto
    {
        public long ItemId { get; set; }
        public string ItemCode { get; set; } = string.Empty;
        public string ItemName { get; set; } = string.Empty;
        public string BatchNumber { get; set; } = string.Empty;
        public DateTime ExpiryDate { get; set; }
        public decimal ReceivedQuantity { get; set; }
        public decimal AcceptedQuantity { get; set; }
        public decimal RejectedQuantity { get; set; }
        public string QcDecision { get; set; } = "Approved";
        public string QcStatus { get; set; } = "Approved";
        public string RejectionReason { get; set; } = string.Empty;
        public decimal UnitPrice { get; set; }
        public decimal UnitCost { get; set; }
    }

    // ========================================================
    // 2. DTOs فواتير وأوامر الشراء (تعتمد عليها PurchasingService)
    // ========================================================
    public class PurchaseOrderLookupDto
    {
        public long Id { get; set; }
        public string OrderNumber { get; set; } = string.Empty;
        public string OrderDate { get; set; } = string.Empty;
        public long SupplierId { get; set; }
        public string? SupplierName { get; set; }
        public long WarehouseId { get; set; }
        public string? WarehouseName { get; set; }
        public decimal TotalAmount { get; set; }
        public string Status { get; set; } = string.Empty;
        public List<PurchaseOrderLineDto> Lines { get; set; } = new();
    }

    public class PurchaseOrderLineDto
    {
        public long Id { get; set; }
        public long ItemId { get; set; }
        public string? ItemCode { get; set; }
        public string? ItemName { get; set; }
        public string? Unit { get; set; } = "كجم";

        // الكميات المطلوبة والمستلمة (دعم الاسمين معاً)
        public decimal Quantity { get; set; }
        public decimal OrderedQuantity { get; set; }
        public decimal ReceivedQuantity { get; set; }
        public decimal RemainingQuantity { get; set; }

        // التكلفة والسعر (دعم الاسمين معاً)
        public decimal UnitPrice { get; set; }
        public decimal UnitCost { get; set; }

        // اشتراطات الصلاحية والتتبع (GMP)
        public bool HasExpiryDate { get; set; }
        public int? ShelfLifeDays { get; set; }

        public string? Notes { get; set; }
    }

    // للتوافق مع أي استدعاء قديم
    public class PurchaseOrderLineLookupDto : PurchaseOrderLineDto { }

    // ========================================================
    // 3. DTO إذن استلام الخامات العام (PurchasingController & Service)
    // ========================================================
    public class MaterialReceiptDto
    {
        public long Id { get; set; }
        public string ReceiptNumber { get; set; } = string.Empty;
        public long PurchaseOrderId { get; set; }
        public string? PurchaseOrderNumber { get; set; }
        public long SupplierId { get; set; }
        public string? SupplierName { get; set; }
        public long WarehouseId { get; set; }
        public string? WarehouseName { get; set; }
        public DateTime ReceiptDate { get; set; }
        public string? InspectorName { get; set; }
        public string? InspectorCode { get; set; }
        public string? QcInspectorName { get; set; }
        public string? CoaNumber { get; set; }
        public string Status { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public string? Notes { get; set; }
        public List<MaterialReceiptLineDto> Lines { get; set; } = new();
    }

    public class MaterialReceiptLineDto
    {
        public long Id { get; set; }
        public long MaterialReceiptId { get; set; }
        public long ItemId { get; set; }
        public string? ItemCode { get; set; }
        public string? ItemName { get; set; }
        public string? Unit { get; set; }
        public decimal Quantity { get; set; }
        public decimal ReceivedQuantity { get; set; }
        public decimal AcceptedQuantity { get; set; }
        public decimal RejectedQuantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal UnitCost { get; set; }
        public string? BatchNumber { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public bool HasExpiryDate { get; set; }
        public int? ShelfLifeDays { get; set; }
        public string? QcDecision { get; set; }
        public string? QcStatus { get; set; }
        public string? RejectionReason { get; set; }
    }
}