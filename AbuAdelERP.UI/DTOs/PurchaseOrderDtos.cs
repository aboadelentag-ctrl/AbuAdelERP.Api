using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace AbuAdelERP.UI.DTOs
{
    public class PurchaseOrderDto
    {
        public long Id { get; set; }
        public long CompanyId { get; set; }
        public long SupplierId { get; set; }
        public string SupplierName { get; set; } = string.Empty;
        public long WarehouseId { get; set; }
        public string OrderNumber { get; set; } = string.Empty;
        public DateTime OrderDate { get; set; }
        public string Status { get; set; } = "Pending_Procurement";
        public decimal Subtotal { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal TotalAmount { get; set; }
        public List<PurchaseOrderDetailDto> Lines { get; set; } = new();
    }

    public class PurchaseOrderDetailDto
    {
        public long Id { get; set; }
        public long PurchaseOrderId { get; set; }
        public long ItemId { get; set; }
        public string ItemCode { get; set; } = string.Empty;
        public string ItemName { get; set; } = string.Empty;
        public string Unit { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal ReceivedQuantity { get; set; }
        public string? Notes { get; set; }
    }

    public class PurchaseOrderCreateDto
    {
        public long CompanyId { get; set; } = 1;

        [Required(ErrorMessage = "يرجى اختيار المورد.")]
        public long SupplierId { get; set; }

        [Required(ErrorMessage = "يرجى تحديد مستودع الاستلام.")]
        public long WarehouseId { get; set; }

        public string? OrderNumber { get; set; }
        public DateTime OrderDate { get; set; } = DateTime.Today;
        public string Status { get; set; } = "Pending_Procurement";
        public List<PurchaseOrderCreateLineDto> Lines { get; set; } = new();
    }

    public class PurchaseOrderCreateLineDto
    {
        public long ItemId { get; set; }
        public decimal Quantity { get; set; } = 1;
        public decimal UnitPrice { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal TaxAmount { get; set; }
        public string? Notes { get; set; }
        public decimal LineTotal => (Quantity * UnitPrice) - DiscountAmount + TaxAmount;
    }

    public class PurchaseOrderResultDto
    {
        public bool Success { get; set; }
        public long Id { get; set; }
        public string OrderNumber { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }

    public class PoApproveDto
    {
        public long OrderId { get; set; }
        public string Status { get; set; } = "Approved";
        public List<PoApproveLineDto> Lines { get; set; } = new();
    }

    public class PoApproveLineDto
    {
        public long LineId { get; set; }
        public long ItemId { get; set; }
        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public string? Notes { get; set; }
    }

    public class PurchaseOrderLookupsDto
    {
        public List<PoSupplierLookupDto> Suppliers { get; set; } = new();
        public List<PoWarehouseLookupDto> Warehouses { get; set; } = new();
        public List<PoItemLookupDto> Items { get; set; } = new();
    }

    public class PoSupplierLookupDto
    {
        public long Id { get; set; }
        public string SupplierCode { get; set; } = string.Empty;
        public string SupplierName { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string? PaymentTerms { get; set; }
    }

    public class PoWarehouseLookupDto
    {
        public long Id { get; set; }
        public string WarehouseCode { get; set; } = string.Empty;
        public string WarehouseName { get; set; } = string.Empty;
    }

    public class PoItemLookupDto
    {
        public long Id { get; set; }
        public string ItemCode { get; set; } = string.Empty;
        public string ItemName { get; set; } = string.Empty;
        public string Unit { get; set; } = string.Empty;
        public decimal BasePrice { get; set; }
        public string CategoryName { get; set; } = string.Empty;
    }
}