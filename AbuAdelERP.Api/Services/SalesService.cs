using System;
using System.Linq;
using System.Threading.Tasks;
using AbuAdelERP.Api.Models;
using AbuAdelERP.Api.DTOs;

namespace AbuAdelERP.Api.Services;

public class SalesService : ISalesService
{
    private readonly AppDbContext _context;

    public SalesService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<object> CreateInvoiceAsync(SalesInvoiceCreateDto dto)
    {
        if (dto.InvoiceLines == null || !dto.InvoiceLines.Any())
            throw new Exception("يجب إضافة صنف واحد على الأقل لإنشاء الفاتورة.");

        // توليد رقم فاتورة آلي (مثال: INV-20260907143000)
        string invoiceNumber = $"INV-{DateTime.Now:yyyyMMddHHmmss}";
        decimal finalTotalAmount = 0;

        var invoice = new SalesInvoice
        {
            InvoiceNumber = invoiceNumber,
            InvoiceDate = DateTime.UtcNow,
            CustomerId = dto.CustomerId,
            TotalAmount = 0 // سنقوم بتحديثه بعد حساب السطور
        };

        foreach (var line in dto.InvoiceLines)
        {
            decimal lineTotal = line.Quantity * line.UnitPrice;
            finalTotalAmount += lineTotal;

            invoice.InvoiceLines.Add(new SalesInvoiceLine
            {
                ItemId = line.ItemId,
                Quantity = line.Quantity,
                UnitPrice = line.UnitPrice,
                LineTotal = lineTotal
            });

            // ملاحظة محاسبية: في هذه النقطة يتم كتابة كود خصم الكمية المباعة من مخزن الإنتاج التام
        }

        invoice.TotalAmount = finalTotalAmount;

        await _context.SalesInvoices.AddAsync(invoice);
        await _context.SaveChangesAsync();

        return new
        {
            Message = "تم إصدار فاتورة المبيعات بنجاح",
            InvoiceNumber = invoice.InvoiceNumber,
            TotalAmount = invoice.TotalAmount,
            ItemsCount = invoice.InvoiceLines.Count
        };
    }
}