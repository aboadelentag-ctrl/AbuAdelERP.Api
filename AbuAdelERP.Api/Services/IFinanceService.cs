using System.Threading.Tasks;

namespace AbuAdelERP.Api.Services;

public interface IFinanceService
{
    Task<object> GetLatestJournalEntriesAsync();
}