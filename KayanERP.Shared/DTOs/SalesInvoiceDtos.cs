using System;
using System.Collections.Generic;

namespace KayanERP.Shared.DTOs;

// ==============================================================
// SalesInvoiceCreateDto — DTO رئيسي لإنشاء فاتورة مبيعات
// ==============================================================
public class SalesInvoiceCreateDto
{
    public long CompanyId { get; set; }
    public long BranchId { get; set; }
    public long CustomerId { get; set; }
    public long WarehouseId { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public DateTime InvoiceDate { get; set; } = DateTime.UtcNow;
    public List<SalesInvoiceLineCreateDto> InvoiceLines { get; set; } = new();
    public string? Notes { get; set; }
}

public class SalesInvoiceLineCreateDto
{
    public long ItemId { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TaxAmount { get; set; }
}

// ==============================================================
// ExtendedSalesInvoiceCreateDto — DTO ممتد مع خيارات السداد
// ==============================================================
public class ExtendedSalesInvoiceCreateDto : SalesInvoiceCreateDto
{
    public string PaymentMethod { get; set; } = "CASH"; // CASH (نقدي) | CREDIT (آجل على الحساب)
    public string? FinancialNotes { get; set; }
}