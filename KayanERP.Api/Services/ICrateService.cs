using System.Collections.Generic;
using System.Threading.Tasks;
using KayanERP.Api.DTOs;

namespace KayanERP.Api.Services
{
    public interface ICrateService
    {
        // 1. الدوال المتوافقة مع النظام السابق
        Task<object> ProcessTransactionAsync(CrateTransactionCreateDto dto);
        Task<object> GetPartyBalanceAsync(string partyType, long partyId, long crateId);

        // 2. عمليات الحاويات والأسبات المتداولة (الداخلية)
        Task<CrateLookupsDto> GetLookupsAsync();
        Task<List<CirculatingContainerViewDto>> GetInventoryAsync();
        Task<List<CrateMovementViewDto>> GetMovementsAsync(string? status);
        Task<CrateMovementViewDto> DispatchCratesAsync(DispatchCrateDto dto, string fallbackEmpCode, string fallbackEmpName);
        Task<(bool Success, string Message, CrateMovementViewDto? Movement)> ReceiveCratesAsync(
            long id, ReceiveCrateDto dto, string fallbackEmpCode, string fallbackEmpName);
        Task<(bool Success, string Message, CrateMovementViewDto? Movement)> SettleDisputeAsync(
            long id, SettleDisputeDto dto, string fallbackEmpCode, string fallbackEmpName);

        // 3. عمليات عهد الصناديق مع العملاء (الخارجية)
        Task<int> GetFactoryBalanceAsync();
        Task<List<CrateClientTransactionViewDto>> GetClientTransactionsAsync(long clientId);   // ✅ int → long
        Task<CrateClientTransactionViewDto> AddClientTransactionAsync(CreateCrateClientTransactionDto dto);
    }
}