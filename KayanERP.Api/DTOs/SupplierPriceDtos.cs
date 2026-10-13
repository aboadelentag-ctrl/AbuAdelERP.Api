using System;
using System.ComponentModel.DataAnnotations;

namespace KayanERP.Api.DTOs
{
    public class SupplierItemPriceDto
    {
        public long Id { get; set; }
        public long SupplierId { get; set; }
        public string SupplierName { get; set; } = string.Empty;
        public string SupplierCode { get; set; } = string.Empty;
        public long ItemId { get; set; }
        public string ItemCode { get; set; } = string.Empty;
        public string ItemName { get; set; } = string.Empty;
        public string Unit { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string Currency { get; set; } = "EGP";
        public int LeadTimeDays { get; set; }
        public int DeliveryDays { get; set; }
        public string EffectiveDate { get; set; } = string.Empty;
        public bool IsPreferred { get; set; }
        public string? Notes { get; set; }
    }

    public class CreateSupplierPriceDto
    {
        [Required(ErrorMessage = "يجب تحديد المورد.")]
        [Range(1, long.MaxValue, ErrorMessage = "معرف المورد غير صالح.")]
        public long SupplierId { get; set; }

        [Required(ErrorMessage = "يجب تحديد الصنف.")]
        [Range(1, long.MaxValue, ErrorMessage = "معرف الصنف غير صالح.")]
        public long ItemId { get; set; }

        [Required(ErrorMessage = "يجب تحديد سعر التوريد.")]
        [Range(0.0001, double.MaxValue, ErrorMessage = "السعر يجب أن يكون أكبر من الصفر.")]
        public decimal Price { get; set; }

        public string Currency { get; set; } = "EGP";
        public int LeadTimeDays { get; set; } = 1;
        public int DeliveryDays { get; set; } = 1;
        public string? EffectiveDate { get; set; }
        public bool IsPreferred { get; set; }
        public string? Notes { get; set; }
    }

    public class SupplierPriceOperationResultDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public long Id { get; set; }
    }
}