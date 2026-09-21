using System.Collections.Generic;
using System.Threading.Tasks;
using AbuAdelERP.Api.DTOs;

namespace AbuAdelERP.Api.Services
{
    public interface IProductionOrderService
    {
        Task<List<ProductionOrderListDto>> GetAllOrdersAsync();
        Task<ProductionOrderListDto?> GetOrderByIdAsync(long id);
        Task<bool> CreateOrderAsync(CreateProductionOrderDto dto);
    }
}