namespace KayanERP.Api.DTOs;

/// <summary>
/// DTO ممتد لإنشاء فاتورة مبيعات مع خيارات السداد.
/// </summary>
public class ExtendedSalesInvoiceCreateDto : SalesInvoiceCreateDto
{
    public string PaymentMethod { get; set; } = "CASH"; // CASH (نقدي) | CREDIT (آجل على الحساب)
    public string? FinancialNotes { get; set; }
}