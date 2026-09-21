using System.Threading.Tasks;
using AbuAdelERP.Api.DTOs;

namespace AbuAdelERP.Api.Services;

public interface ICrateService
{
    Task<object> ProcessTransactionAsync(CrateTransactionCreateDto dto);
    Task<object> GetPartyBalanceAsync(string partyType, long partyId, long crateId);
}