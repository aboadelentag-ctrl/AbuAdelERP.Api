using System.Threading.Tasks;
using AbuAdelERP.Api.DTOs;

namespace AbuAdelERP.Api.Services;

public interface ISalesService
{
    Task<object> CreateInvoiceAsync(SalesInvoiceCreateDto dto);
}