using System.Threading.Tasks;

namespace KayanERP.Api.Services;

public interface IFinanceService
{
    Task<object> GetLatestJournalEntriesAsync();
}