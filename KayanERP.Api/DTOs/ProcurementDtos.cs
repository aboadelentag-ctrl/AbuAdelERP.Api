namespace KayanERP.Api.DTOs // في مشروع الـ UI اجعل الـ Namespace: KayanERP.UI.Models
{
    public class ProcurementPendingLineDto
    {
        public long LineId { get; set; }
        public long ItemId { get; set; }
        public string ItemCode { get; set; } = string.Empty;
        public string ItemName { get; set; } = string.Empty;
        public string Unit { get; set; } = string.Empty;
        public decimal RequestedQuantity { get; set; }
        public decimal UnitPrice { get; set; } = 0;
        public decimal DiscountAmount { get; set; } = 0;
        public decimal TaxAmount { get; set; } = 0;
        public long SupplierId { get; set; } = 0; // تم إضافة المورد لكل سطر صنف
        public decimal LineTotal => (RequestedQuantity * UnitPrice) - DiscountAmount + TaxAmount;
    }

    public class ProcurementPendingRequestDto
    {
        public long Id { get; set; }
        public string RequestNumber { get; set; } = string.Empty;
        public long WarehouseId { get; set; }
        public string WarehouseName { get; set; } = string.Empty;
        public DateTime RequestDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public int ItemsCount { get; set; }
        public List<ProcurementPendingLineDto> Lines { get; set; } = new();
    }

    public class ProcurementConvertLineDto
    {
        public long LineId { get; set; }
        public long ItemId { get; set; }
        public long SupplierId { get; set; } // المورد المحدد للصنف
        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal DiscountAmount { get; set; } = 0;
        public decimal TaxAmount { get; set; } = 0;
    }

    public class ProcurementConvertDto
    {
        public long RequestId { get; set; }
        public string InvoiceNumberPrefix { get; set; } = string.Empty;
        public DateTime InvoiceDate { get; set; } = DateTime.Today;
        public List<ProcurementConvertLineDto> Lines { get; set; } = new();
    }

    public class ProcurementApprovedInvoiceDto
    {
        public long Id { get; set; }
        public string InvoiceNumber { get; set; } = string.Empty;
        public string SupplierName { get; set; } = string.Empty;
        public string WarehouseName { get; set; } = string.Empty;
        public DateTime InvoiceDate { get; set; }
        public decimal TotalAmount { get; set; }
        public string Status { get; set; } = string.Empty;
    }
}