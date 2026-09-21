using AbuAdelERP.Api.Models;
using AbuAdelERP.Api.DTOs;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AbuAdelERP.Api.Services
{
    public class StockService : IStockService
    {
        private readonly AppDbContext _context;

        public StockService(AppDbContext context)
        {
            _context = context;
        }

        // 1. إضافة رصيد افتتاحي أو حركة مخزنية مع التحقق من وجود الشركة
        public async Task<object> AddInitialStockAsync(InitialStockDto dto)
        {
            if (dto == null)
                throw new ArgumentNullException(nameof(dto), "بيانات الإذن غير صالحة.");

            // استخدام الشركة الأولى المتاحة في قاعدة البيانات تلقائياً لضمان عدم حدوث خطأ Foreign Key
            var defaultCompany = await _context.Companies.FirstOrDefaultAsync();
            long companyId = defaultCompany != null ? defaultCompany.Id : 1;

            var movement = new StockMovement
            {
                CompanyId = companyId,
                ItemId = dto.ItemId,
                WarehouseId = dto.WarehouseId,
                MovementType = "INITIAL_BALANCE",
                Quantity = dto.Quantity,
                UnitCost = dto.UnitCost,
                Notes = dto.Notes ?? string.Empty,
                ReferenceType = "MANUAL",
                MovementAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            };

            await _context.StockMovements.AddAsync(movement);

            var balance = await _context.StockBalances
                .FirstOrDefaultAsync(b => b.ItemId == dto.ItemId && b.WarehouseId == dto.WarehouseId);

            if (balance == null)
            {
                balance = new StockBalance
                {
                    CompanyId = companyId,
                    ItemId = dto.ItemId,
                    WarehouseId = dto.WarehouseId,
                    Quantity = dto.Quantity,
                    ReservedQuantity = 0,
                    LastUpdatedAt = DateTime.UtcNow
                };
                await _context.StockBalances.AddAsync(balance);
            }
            else
            {
                balance.Quantity += dto.Quantity;
                balance.LastUpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
            return new { Message = "تم إضافة الرصيد بنجاح وتحديث المخازن!", CurrentBalance = balance.Quantity };
        }

        // 2. استعلام عن أرصدة المخازن الحالية
        public async Task<IEnumerable<object>> GetStockBalancesAsync(long? warehouseId = null)
        {
            var query = _context.StockBalances.AsQueryable();

            if (warehouseId.HasValue)
                query = query.Where(b => b.WarehouseId == warehouseId.Value);

            var balances = await query
                .OrderBy(b => b.WarehouseId)
                .ThenBy(b => b.ItemId)
                .Select(b => new
                {
                    WarehouseId = b.WarehouseId,
                    ItemId = b.ItemId,
                    Quantity = b.Quantity,
                    LastUpdatedAt = b.LastUpdatedAt
                }).ToListAsync();

            return balances;
        }

        // 3. استخراج كارت الصنف (تفاصيل كل حركة دخول وخروج)
        public async Task<IEnumerable<object>> GetItemLedgerAsync(long itemId)
        {
            var movements = await _context.StockMovements
                .Where(m => m.ItemId == itemId)
                .OrderByDescending(m => m.MovementAt)
                .Select(m => new
                {
                    MovementDate = m.MovementAt,
                    WarehouseId = m.WarehouseId,
                    MovementType = m.MovementType,
                    Quantity = m.Quantity,
                    ReferenceType = m.ReferenceType,
                    ReferenceId = m.ReferenceId,
                    Notes = m.Notes
                }).ToListAsync();

            return movements;
        }
    }
}