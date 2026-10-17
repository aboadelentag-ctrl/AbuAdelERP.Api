using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace KayanERP.Shared.DTOs;

public class PurchaseInvoiceCreateDto
{
    [Required]
    public long SupplierId { get; set; } // رقم مورد الخامات

    [Required]
    public List<PurchaseInvoiceLineDto> InvoiceLines { get; set; } = new List<PurchaseInvoiceLineDto>();
}

public class PurchaseInvoiceLineDto
{
    [Required]
    public long ItemId { get; set; } // رقم الخامة (دقيق، سكر..)

    [Required]
    [Range(0.01, double.MaxValue, ErrorMessage = "الكمية يجب أن تكون أكبر من صفر")]
    public decimal Quantity { get; set; }

    [Required]
    [Range(0.01, double.MaxValue, ErrorMessage = "السعر يجب أن يكون أكبر من صفر")]
    public decimal UnitPrice { get; set; }
}