using System.Threading.Tasks;
using KayanERP.Api.DTOs;

namespace KayanERP.Api.Services;

public interface IAccountService
{
    Task<object> CreateAccountAsync(AccountCreateDto dto);
    Task<object> GetAllAccountsAsync();
}