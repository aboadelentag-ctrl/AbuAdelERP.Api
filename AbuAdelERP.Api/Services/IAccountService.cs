using System.Threading.Tasks;
using AbuAdelERP.Api.DTOs;

namespace AbuAdelERP.Api.Services;

public interface IAccountService
{
    Task<object> CreateAccountAsync(AccountCreateDto dto);
    Task<object> GetAllAccountsAsync();
}