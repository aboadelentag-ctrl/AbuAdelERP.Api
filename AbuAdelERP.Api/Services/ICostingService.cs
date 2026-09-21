using System.Threading.Tasks;
using AbuAdelERP.Api.DTOs;

namespace AbuAdelERP.Api.Services;

public interface ICostingService
{
    Task<object> CalculateBatchCostAsync(BatchCostRequestDto dto);
}