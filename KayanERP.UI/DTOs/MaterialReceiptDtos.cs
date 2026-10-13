using System;
using System.Collections.Generic;

namespace KayanERP.UI.DTOs
{
    public class MaterialReceiptLookupsDto
    {
        public List<MaterialReceiptSupplierLookupDto> Suppliers { get; set; } = new();
        public List<MaterialReceiptWarehouseLookupDto> Warehouses { get; set; } = new();
    }

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

    public class PendingInvoiceLineDto
    {
        public long Id { get; set; }
        public long ItemId { get; set; }
        public string ItemCode { get; set; } = string.Empty;
        public string ItemName { get; set; } = string.Empty;
        public string Unit { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public decimal ReceivedQuantity { get; set; }
        public decimal RemainingQuantity { get; set; }
        public decimal UnitPrice { get; set; }
        public string? Notes { get; set; }
    }

    public class MaterialReceiptPostDto
    {
        public long InvoiceId { get; set; }
        public long WarehouseId { get; set; }
        public string InspectorCode { get; set; } = "QC-01";
        public string InspectorName { get; set; } = "مفتش الجودة المخزنية";
        public string CoaNumber { get; set; } = string.Empty;
        public List<MaterialReceiptItemPostDto> Items { get; set; } = new();
    }

    public class MaterialReceiptItemPostDto
    {
        public long ItemId { get; set; }
        public string ItemName { get; set; } = string.Empty;
        public string ItemCode { get; set; } = string.Empty;
        public string Unit { get; set; } = string.Empty;
        public decimal RemainingQuantity { get; set; }
        public decimal AcceptedQuantity { get; set; }
        public decimal RejectedQuantity { get; set; }
        public string BatchNumber { get; set; } = string.Empty;
        public DateTime ExpiryDate { get; set; } = DateTime.Today.AddYears(1);
        public decimal UnitPrice { get; set; }
        public string QcDecision { get; set; } = "Accepted";
        public string RejectionReason { get; set; } = "مطابق للمواصفة";
    }

    public class MaterialReceiptResultDto
    {
        public bool Success { get; set; }
        public string NewStatus { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }
}