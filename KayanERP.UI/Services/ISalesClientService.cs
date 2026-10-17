using System.Collections.Generic;
using System.Threading.Tasks;
using KayanERP.Shared.DTOs;

namespace KayanERP.UI.Services
{
    public interface ISalesClientService
    {
        // ==========================================
        // 1. دورة فواتير المبيعات
        // ==========================================
        Task<List<SalesInvoiceViewDto>> GetAllInvoicesAsync();
        Task<SalesInvoiceViewDto?> GetInvoiceByIdAsync(long id);
        Task<SalesLookupsDto> GetLookupsAsync();
        Task<SalesInvoiceResultDto> CreateInvoiceAsync(SalesInvoiceCreateDto dto);

        // ==========================================
        // 2. دليل العملاء ومنافذ التوزيع
        // ==========================================
        Task<List<CustomerItemDto>> GetAllCustomersAsync();
        Task<bool> SaveCustomerAsync(CustomerItemDto customer);
        Task<bool> ToggleCustomerStatusAsync(long id);
    }
}