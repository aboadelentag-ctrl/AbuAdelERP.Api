using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace KayanERP.Api.DTOs
{
    // ==============================================================
    // 1. نماذج استعراض أمر الشراء والبنود
    // ==============================================================
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
        public decimal LineTotal => (Quantity * UnitPrice) - DiscountAmount + TaxAmount;
        public string? Notes { get; set; }
    }

    public class PurchaseOrderDto
    {
        public long Id { get; set; }
        public long CompanyId { get; set; }
        public long SupplierId { get; set; }
        public string SupplierName { get; set; } = string.Empty;
        public long WarehouseId { get; set; }
        public string OrderNumber { get; set; } = string.Empty;
        public DateTime OrderDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public decimal Subtotal { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal TotalAmount { get; set; }
        public List<PurchaseOrderDetailDto> Lines { get; set; } = new();
    }

    // ==============================================================
    // 2. نماذج إنشاء أمر الشراء / طلب الاحتياج
    // ==============================================================
    public class PurchaseOrderLineCreateDto
    {
        [Required(ErrorMessage = "يجب تحديد الصنف.")]
        public long ItemId { get; set; }

        [Range(0.001, double.MaxValue, ErrorMessage = "الكمية يجب أن تكون أكبر من الصفر.")]
        public decimal Quantity { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "السعر لا يمكن أن يكون سالباً.")]
        public decimal UnitPrice { get; set; }

        public decimal DiscountAmount { get; set; } = 0;
        public decimal TaxAmount { get; set; } = 0;
        public string? Notes { get; set; }
    }

    public class PurchaseOrderCreateDto
    {
        public long CompanyId { get; set; } = 1;
        public long SupplierId { get; set; } = 0;
        public long WarehouseId { get; set; } = 1;
        public string OrderNumber { get; set; } = string.Empty;
        public DateTime OrderDate { get; set; } = DateTime.Today;
        public string Status { get; set; } = "Pending_Procurement";

        [Required(ErrorMessage = "يجب إدراج بند واحد على الأقل في أمر الشراء.")]
        public List<PurchaseOrderLineCreateDto> Lines { get; set; } = new();
    }

    // ==============================================================
    // 3. نماذج اعتماد الفاتورة وتخصيص الموردين والأسعار
    // ==============================================================
    public class PoApproveLineDto
    {
        public long LineId { get; set; }
        public long ItemId { get; set; }
        public long SupplierId { get; set; }
        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public string? Notes { get; set; }
    }

    public class PoApproveDto
    {
        [Required]
        public long OrderId { get; set; }
        public string Status { get; set; } = "Approved";

        [Required(ErrorMessage = "يجب إدراج سطور الاعتماد.")]
        public List<PoApproveLineDto> Lines { get; set; } = new();
    }

    public class PurchaseOrderResultDto
    {
        public bool Success { get; set; }
        public long Id { get; set; }
        public string OrderNumber { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }
}