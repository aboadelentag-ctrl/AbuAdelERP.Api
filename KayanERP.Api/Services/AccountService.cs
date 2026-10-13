using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using KayanERP.Api.Models;
using KayanERP.Api.DTOs;

namespace KayanERP.Api.Services;

public class AccountService : IAccountService
{
    private readonly AppDbContext _context;

    public AccountService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<object> CreateAccountAsync(AccountCreateDto dto)
    {
        // التأكد من أن كود الحساب غير متكرر
        var exists = await _context.Accounts.AnyAsync(a => a.AccountCode == dto.AccountCode);
        if (exists)
            throw new Exception("عفواً، كود الحساب هذا مسجل مسبقاً في الدليل المحاسبي.");

        var account = new Account
        {
            CompanyId = 6,
            AccountCode = dto.AccountCode,
            AccountName = dto.AccountName,
            AccountType = dto.AccountType.ToUpper(),
            ParentAccountId = dto.ParentAccountId,
            Active = true,
            CreatedAt = DateTime.UtcNow
        };

        await _context.Accounts.AddAsync(account);
        await _context.SaveChangesAsync();

        return new { Message = "تم إنشاء الحساب المالي بنجاح!", AccountId = account.Id };
    }

    public async Task<object> GetAllAccountsAsync()
    {
        return await _context.Accounts
            .Select(a => new
            {
                a.Id,
                a.AccountCode,
                a.AccountName,
                a.AccountType
            })
            .OrderBy(a => a.AccountCode)
            .ToListAsync();
    }
}