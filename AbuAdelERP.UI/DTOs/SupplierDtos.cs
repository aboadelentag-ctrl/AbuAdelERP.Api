using System;
using System.ComponentModel.DataAnnotations;

namespace AbuAdelERP.UI.DTOs
{
    public class SupplierViewDto
    {
        public long Id { get; set; }
        public long CompanyId { get; set; }
        public string SupplierCode { get; set; } = string.Empty;
        public string SupplierName { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public string? Address { get; set; }
        public string? TaxNumber { get; set; }
        public string? PaymentTerms { get; set; }
        public bool Active { get; set; } = true;
        public string? Notes { get; set; }
        public DateTime? CreatedAt { get; set; }
    }

    public class SupplierCreateUpdateDto
    {
        public string? SupplierCode { get; set; }

        [Required(ErrorMessage = "اسم المورد مطلوب.")]
        public string SupplierName { get; set; } = string.Empty;

        public string? Phone { get; set; }
        public string? Email { get; set; }
        public string? Address { get; set; }
        public string? TaxNumber { get; set; }
        public string? PaymentTerms { get; set; } = "نقدي";
        public bool Active { get; set; } = true;
        public string? Notes { get; set; }
    }
}