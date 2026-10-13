using System.Collections.Generic;
using System.Threading.Tasks;
using KayanERP.Api.DTOs;

namespace KayanERP.Api.Services
{
    public interface ISalesService
    {
        Task<SalesLookupsDto> GetLookupsAsync();
        Task<List<SalesInvoiceViewDto>> GetAllInvoicesAsync();
        Task<SalesInvoiceViewDto?> GetInvoiceByIdAsync(long id);
        Task<SalesInvoiceResultDto> CreateInvoiceAsync(SalesInvoiceCreateDto dto);
    }
}