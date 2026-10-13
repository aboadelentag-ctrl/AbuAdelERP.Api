namespace KayanERP.UI.Models
{
    public class PurchaseRequestItemLookupDto
    {
        public long Id { get; set; }
        public string ItemCode { get; set; } = string.Empty;
        public string ItemName { get; set; } = string.Empty;
        public string Unit { get; set; } = string.Empty;
    }

    public class PurchaseRequestDepartmentLookupDto
    {
        public long Id { get; set; }
        public string DepartmentName { get; set; } = string.Empty;
    }

    public class PurchaseRequestWarehouseLookupDto
    {
        public long Id { get; set; }
        public string WarehouseName { get; set; } = string.Empty;
    }

    public class PurchaseRequestDto
    {
        public long Id { get; set; }
        public string RequestNumber { get; set; } = string.Empty;
        public long? DepartmentId { get; set; }
        public string DepartmentName { get; set; } = string.Empty;
        public long? WarehouseId { get; set; }
        public string WarehouseName { get; set; } = string.Empty;
        public DateTime RequestDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;
    }

    public class PurchaseRequestLineCreateDto
    {
        public long ItemId { get; set; }
        public decimal RequestedQuantity { get; set; } = 1;
        public string Notes { get; set; } = string.Empty;
    }

    public class PurchaseRequestCreateDto
    {
        public long CompanyId { get; set; } = 1;
        public string RequestNumber { get; set; } = string.Empty;
        public long? DepartmentId { get; set; }
        public long? WarehouseId { get; set; }
        public DateTime RequestDate { get; set; } = DateTime.Today;
        public string Notes { get; set; } = string.Empty;
        public List<PurchaseRequestLineCreateDto> Lines { get; set; } = new();
    }
}