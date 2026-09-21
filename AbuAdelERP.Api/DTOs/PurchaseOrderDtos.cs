namespace AbuAdelERP.Api.DTOs
{
    public class PurchaseOrderDetailDto
    {
        public long Id { get; set; }
        public long PurchaseOrderId { get; set; }
        public long ItemId { get; set; }
        public string ItemName { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal ReceivedQuantity { get; set; }
        public decimal LineTotal => (Quantity * UnitPrice) - DiscountAmount + TaxAmount;
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

    public class PurchaseOrderLineCreateDto
    {
        public long ItemId { get; set; }
        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal DiscountAmount { get; set; } = 0;
        public decimal TaxAmount { get; set; } = 0;
    }

    public class PurchaseOrderCreateDto
    {
        public long CompanyId { get; set; } = 1;
        public long SupplierId { get; set; }
        public long WarehouseId { get; set; } = 1;
        public string OrderNumber { get; set; } = string.Empty;
        public DateTime OrderDate { get; set; } = DateTime.Today;
        public string Status { get; set; } = "Approved";
        public List<PurchaseOrderLineCreateDto> Lines { get; set; } = new();
    }
}