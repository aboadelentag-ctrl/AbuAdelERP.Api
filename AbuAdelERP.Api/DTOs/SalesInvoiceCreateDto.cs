using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace AbuAdelERP.Api.DTOs;

public class SalesInvoiceCreateDto
{
    [Required]
    public long CustomerId { get; set; } // رقم العميل أو الفرع

    [Required]
    public List<SalesInvoiceLineDto> InvoiceLines { get; set; } = new List<SalesInvoiceLineDto>();
}

public class SalesInvoiceLineDto
{
    [Required]
    public long ItemId { get; set; } // رقم المنتج المباع

    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "الكمية يجب أن تكون أكبر من صفر")]
    public int Quantity { get; set; }

    [Required]
    [Range(0.01, double.MaxValue, ErrorMessage = "السعر يجب أن يكون أكبر من صفر")]
    public decimal UnitPrice { get; set; }
}