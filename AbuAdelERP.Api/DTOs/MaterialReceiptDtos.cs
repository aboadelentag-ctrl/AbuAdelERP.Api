using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace AbuAdelERP.Api.DTOs
{
    // ==============================================================
    // 1. القوائم المرجعية لاستلام الخامات
    // ==============================================================
    public class MaterialReceiptSupplierLookupDto
    {
        public long Id { get; set; }
        public string SupplierName { get; set; } = string.Empty;
    }

    public class MaterialReceiptWarehouseLookupDto
    {
        public long Id { get; set; }
        public string WarehouseName { get; set; } = string.Empty;
    }

    public class MaterialReceiptLookupsDto
    {
        public List<MaterialReceiptSupplierLookupDto> Suppliers { get; set; } = new();
        public List<MaterialReceiptWarehouseLookupDto> Warehouses { get; set; } = new();
    }

    // ==============================================================
    // 2. الفواتير المعتمدة المفتوحة للاستلام
    // ==============================================================
    public class PendingInvoiceLineDto
    {
        public long Id { get; set; }
        public long ItemId { get; set; }
        public string ItemCode { get; set; } = string.Empty;
        public string ItemName { get; set; } = string.Empty;
        public string Unit { get; set; } = "كجم";
        public decimal Quantity { get; set; }
        public decimal ReceivedQuantity { get; set; }
        public decimal RemainingQuantity { get; set; }
        public decimal UnitPrice { get; set; }
        public string? Notes { get; set; }
    }

    public class PendingInvoiceDto
    {
        public long Id { get; set; }
        public string OrderNumber { get; set; } = string.Empty;
        public string OrderDate { get; set; } = string.Empty;
        public long SupplierId { get; set; }
        public string SupplierName { get; set; } = string.Empty;
        public long WarehouseId { get; set; }
        public decimal TotalAmount { get; set; }
        public string Status { get; set; } = string.Empty;
        public List<PendingInvoiceLineDto> Lines { get; set; } = new();
    }

    // ==============================================================
    // 3. تأكيد الاستلام الميداني وفحص الجودة الرقابي
    // ==============================================================
    public class MaterialReceiptItemInputDto
    {
        [Required]
        public long ItemId { get; set; }
        public decimal AcceptedQuantity { get; set; }
        public decimal RejectedQuantity { get; set; }
        public decimal UnitPrice { get; set; }
        public string BatchNumber { get; set; } = string.Empty;
        public DateTime? ExpiryDate { get; set; }
        public string? RejectionReason { get; set; }
        public string? QcDecision { get; set; }
    }

    public class MaterialReceiptPostDto
    {
        [Required]
        public long InvoiceId { get; set; }
        public long WarehouseId { get; set; }
        public string InspectorCode { get; set; } = string.Empty;
        public string InspectorName { get; set; } = string.Empty;
        public string CoaNumber { get; set; } = string.Empty;
        public List<MaterialReceiptItemInputDto> Items { get; set; } = new();
    }

    public class MaterialReceiptResultDto
    {
        public bool Success { get; set; }
        public string NewStatus { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }

    // ==============================================================
    // 4. نماذج التكامل التام مع PurchasingService و PurchasingController
    // ==============================================================
    public class PurchaseOrderLineDto
    {
        public long Id { get; set; }
        public long ItemId { get; set; }
        public string ItemCode { get; set; } = string.Empty;
        public string ItemName { get; set; } = string.Empty;
        public string Unit { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public decimal OrderedQuantity { get; set; }
        public decimal ReceivedQuantity { get; set; }
        public decimal RemainingQuantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal UnitCost { get; set; }
        public bool HasExpiryDate { get; set; }
        public int? ShelfLifeDays { get; set; }
        public string? Notes { get; set; }
    }

    public class PurchaseOrderLineLookupDto : PurchaseOrderLineDto
    {
    }

    public class PurchaseOrderLookupDto
    {
        public long Id { get; set; }
        public string OrderNumber { get; set; } = string.Empty;
        public long SupplierId { get; set; }
        public string SupplierName { get; set; } = string.Empty;
        public long WarehouseId { get; set; }
        public string WarehouseName { get; set; } = string.Empty;
        public string OrderDate { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public List<PurchaseOrderLineDto> Lines { get; set; } = new();
    }

    public class MaterialReceiptDto
    {
        public long PurchaseOrderId { get; set; }
        public long WarehouseId { get; set; }
        public DateTime ReceiptDate { get; set; } = DateTime.UtcNow;
        public string? ReceivedBy { get; set; }
        public string? QcInspectorName { get; set; }
        public string? CoaNumber { get; set; }
        public string? Notes { get; set; }
        public List<MaterialReceiptLineDto> Lines { get; set; } = new();
    }

    public class MaterialReceiptLineDto
    {
        public long ItemId { get; set; }
        public decimal ReceivedQuantity { get; set; }
        public decimal AcceptedQuantity { get; set; }
        public decimal RejectedQuantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal UnitCost { get; set; }
        public string? BatchNumber { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public bool HasExpiryDate { get; set; }
        public bool HadExpiryDate { get; set; }
        public string? QcStatus { get; set; }
        public string? RejectionReason { get; set; }
        public string? Notes { get; set; }
    }
}