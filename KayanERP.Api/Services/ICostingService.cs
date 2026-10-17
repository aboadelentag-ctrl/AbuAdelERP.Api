using System.Threading.Tasks;
using KayanERP.Shared.DTOs;

namespace KayanERP.Api.Services;

public interface ICostingService
{
    Task<object> CalculateBatchCostAsync(BatchCostRequestDto dto);
}