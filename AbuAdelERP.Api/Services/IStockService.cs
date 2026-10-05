using System.Collections.Generic;
using System.Threading.Tasks;
using AbuAdelERP.Api.DTOs;

namespace AbuAdelERP.Api.Services
{
    public interface IStockService
    {
        Task<StockOperationResultDto> AddInitialStockAsync(InitialStockDto dto);
        Task<List<StockBalanceViewDto>> GetStockBalancesAsync(long? warehouseId = null);
        Task<List<StockMovementItemLedgerDto>> GetItemLedgerAsync(long itemId);
    }
}