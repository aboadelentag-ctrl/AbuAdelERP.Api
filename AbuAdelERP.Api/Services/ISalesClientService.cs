using AbuAdelERP.Api.Controllers;
using AbuAdelERP.Api.DTOs;
using AbuAdelERP.UI.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AbuAdelERP.UI.Services
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