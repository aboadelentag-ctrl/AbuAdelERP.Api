namespace KayanERP.UI.Models
{
    public class SupplierDto
    {
        public long Id { get; set; }
        public long CompanyId { get; set; }
        public string SupplierCode { get; set; } = string.Empty;
        public string SupplierName { get; set; } = string.Empty;
        public decimal CreditLimit { get; set; }
        public bool Active { get; set; }
    }

    public class SupplierCreateDto
    {
        public long CompanyId { get; set; } = 1;
        public string SupplierCode { get; set; } = string.Empty;
        public string SupplierName { get; set; } = string.Empty;
        public decimal CreditLimit { get; set; }
        public bool Active { get; set; } = true;
    }
}