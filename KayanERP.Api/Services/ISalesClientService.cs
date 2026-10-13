using KayanERP.Api.Controllers;
using KayanERP.Api.DTOs;
using KayanERP.UI.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace KayanERP.UI.Services
{
    public interface ISalesClientService
    {
        // ==============================================================
        // 1. دورة فواتير المبيعات الصادرة (Sales Invoices)
        // ==============================================================
        Task<List<SalesInvoiceViewDto>> GetAllInvoicesAsync();
        Task<SalesInvoiceViewDto?> GetInvoiceByIdAsync(long id);
        Task<SalesLookupsDto> GetLookupsAsync();
        Task<SalesInvoiceResultDto> CreateInvoiceAsync(SalesInvoiceCreateDto dto);

        // ==============================================================
        // 2. إدارة دليل العملاء ومنافذ التوزيع (Customers Management)
        // ==============================================================
        Task<List<CustomerItemDto>> GetAllCustomersAsync();
        Task<bool> SaveCustomerAsync(CustomerItemDto customer);
        Task<bool> ToggleCustomerStatusAsync(long id);
    }
}