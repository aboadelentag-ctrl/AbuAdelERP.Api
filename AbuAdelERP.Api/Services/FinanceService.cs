using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using AbuAdelERP.Api.Models;

namespace AbuAdelERP.Api.Services;

public class FinanceService : IFinanceService
{
    private readonly AppDbContext _context;

    public FinanceService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<object> GetLatestJournalEntriesAsync()
    {
        // جلب أحدث 10 قيود مع تفاصيل المدين والدائن وأسماء الحسابات
        var entries = await _context.JournalEntries
            .Include(j => j.Lines)
                .ThenInclude(l => l.Account)
            .OrderByDescending(j => j.CreatedAt)
            .Take(10)
            .Select(j => new
            {
                j.EntryNumber,
                j.EntryDate,
                j.ReferenceType,
                j.Notes,
                Lines = j.Lines.Select(l => new
                {
                    AccountCode = l.Account!.AccountCode,
                    AccountName = l.Account.AccountName,
                    Debit = l.Debit,
                    Credit = l.Credit
                }).ToList()
            })
            .ToListAsync();

        return entries;
    }
}