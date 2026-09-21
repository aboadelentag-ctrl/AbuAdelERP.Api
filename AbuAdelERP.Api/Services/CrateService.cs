using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using AbuAdelERP.Api.Models;
using AbuAdelERP.Api.DTOs;

namespace AbuAdelERP.Api.Services;

public class CrateService : ICrateService
{
    private readonly AppDbContext _context;

    public CrateService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<object> ProcessTransactionAsync(CrateTransactionCreateDto dto)
    {
        var crate = await _context.Crates.FindAsync(dto.CrateId);
        if (crate == null) throw new Exception("السبت أو العبوة غير موجودة في النظام.");

        string type = dto.TransactionType.ToUpper();

        // 1. تحديث رصيد المصنع الفعلي
        if (type == "DISPATCH")
        {
            if (crate.TotalFactoryBalance < dto.Quantity)
                throw new Exception($"رصيد المصنع من {crate.CrateName} لا يكفي. المتاح: {crate.TotalFactoryBalance}");

            crate.TotalFactoryBalance -= dto.Quantity; // خروج عهدة من المصنع
        }
        else if (type == "RETURN")
        {
            crate.TotalFactoryBalance += dto.Quantity; // عودة عهدة للمصنع
        }
        // ملاحظة: في حالة DAMAGE (الهالك) لا نزيد رصيد المصنع، بل تسجل الحركة فقط لتسوية عهدة العميل/الفرع.

        // 2. توثيق الحركة
        var transaction = new CrateTransaction
        {
            CrateId = dto.CrateId,
            TransactionDate = DateTime.UtcNow,
            TransactionType = type,
            PartyType = dto.PartyType.ToUpper(),
            PartyId = dto.PartyId,
            Quantity = dto.Quantity,
            Notes = dto.Notes
        };

        await _context.CrateTransactions.AddAsync(transaction);
        await _context.SaveChangesAsync();

        return new
        {
            Message = "تم تسجيل حركة العهدة بنجاح",
            CurrentFactoryBalance = crate.TotalFactoryBalance
        };
    }

    public async Task<object> GetPartyBalanceAsync(string partyType, long partyId, long crateId)
    {
        var crate = await _context.Crates.FindAsync(crateId);
        if (crate == null) throw new Exception("السبت غير موجود");

        // جلب كل حركات هذا الفرع أو العميل لهذا السبت تحديداً
        var transactions = await _context.CrateTransactions
            .Where(t => t.PartyType == partyType.ToUpper() && t.PartyId == partyId && t.CrateId == crateId)
            .ToListAsync();

        int dispatched = transactions.Where(t => t.TransactionType == "DISPATCH").Sum(t => t.Quantity);
        int returned = transactions.Where(t => t.TransactionType == "RETURN").Sum(t => t.Quantity);
        int damaged = transactions.Where(t => t.TransactionType == "DAMAGE").Sum(t => t.Quantity);

        // المعادلة المحاسبية: العهدة المطالب بها = إجمالي المنصرف - (المرتجع السليم + الهالك المثبت)
        int netBalanceDue = dispatched - (returned + damaged);

        return new
        {
            PartyType = partyType.ToUpper(),
            PartyId = partyId,
            CrateName = crate.CrateName,
            TotalDispatched = dispatched,
            TotalReturned = returned,
            TotalDamaged = damaged,
            NetBalanceDue = netBalanceDue
        };
    }
}