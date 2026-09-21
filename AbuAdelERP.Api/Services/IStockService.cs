using AbuAdelERP.Api.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AbuAdelERP.Api.Services;

public interface IStockService
{
    Task<object> AddInitialStockAsync(InitialStockDto dto);
    Task<IEnumerable<object>> GetStockBalancesAsync(long? warehouseId = null);
    Task<IEnumerable<object>> GetItemLedgerAsync(long itemId);
}