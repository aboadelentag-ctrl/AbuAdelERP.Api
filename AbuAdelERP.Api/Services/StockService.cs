using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using AbuAdelERP.Api.Models;
using AbuAdelERP.Api.DTOs;

namespace AbuAdelERP.Api.Services
{
    public class StockService : IStockService
    {
        private readonly AppDbContext _context;

        public StockService(AppDbContext context)
        {
            _context = context;
        }

        // ==============================================================
        // 1. إضافة رصيد افتتاحي ذرياً مع ضبط الرصيد الفعلي والمتاح
        // ==============================================================
        public async Task<StockOperationResultDto> AddInitialStockAsync(InitialStockDto dto)
        {
            if (dto == null)
                throw new ArgumentNullException(nameof(dto), "بيانات الإذن غير صالحة.");

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var defaultCompany = await _context.Companies.AsNoTracking().FirstOrDefaultAsync();
                long companyId = defaultCompany != null ? defaultCompany.Id : 1;

                var now = DateTime.UtcNow;

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
                    MovementAt = now,
                    CreatedAt = now
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
                        AvailableQuantity = dto.Quantity,
                        ReservedQuantity = 0,
                        LastUpdatedAt = now
                    };
                    await _context.StockBalances.AddAsync(balance);
                }
                else
                {
                    balance.Quantity += dto.Quantity;
                    balance.AvailableQuantity = (balance.AvailableQuantity ?? 0m) + dto.Quantity;
                    balance.LastUpdatedAt = now;
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return new StockOperationResultDto
                {
                    Success = true,
                    Message = "تم إضافة الرصيد الافتتاحي بنجاح وتحديث الأرصدة الفعلية والمتاحة.",
                    CurrentBalance = balance.Quantity
                };
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        // ==============================================================
        // 2. استعلام سريع ومجمع لأرصدة المخازن
        // ==============================================================
        public async Task<List<StockBalanceViewDto>> GetStockBalancesAsync(long? warehouseId = null)
        {
            var query = _context.StockBalances
                .AsNoTracking()
                .Include(b => b.Item)
                .Include(b => b.Warehouse)
                .AsQueryable();

            if (warehouseId.HasValue && warehouseId.Value > 0)
                query = query.Where(b => b.WarehouseId == warehouseId.Value);

            return await query
                .OrderBy(b => b.WarehouseId)
                .ThenBy(b => b.ItemId)
                .Select(b => new StockBalanceViewDto
                {
                    WarehouseId = b.WarehouseId,
                    WarehouseName = b.Warehouse != null ? b.Warehouse.WarehouseName : string.Empty,
                    ItemId = b.ItemId,
                    ItemCode = b.Item != null ? b.Item.ItemCode : string.Empty,
                    ItemName = b.Item != null ? b.Item.ItemName : string.Empty,
                    Unit = b.Item != null ? (b.Item.Unit ?? string.Empty) : string.Empty,
                    Quantity = b.Quantity,
                    AvailableQuantity = b.AvailableQuantity ?? 0m,
                    ReservedQuantity = b.ReservedQuantity,
                    LastUpdatedAt = b.LastUpdatedAt
                })
                .ToListAsync();
        }

        // ==============================================================
        // 3. سجل حركات الصنف المباشر
        // ==============================================================
        public async Task<List<StockMovementItemLedgerDto>> GetItemLedgerAsync(long itemId)
        {
            return await _context.StockMovements
                .AsNoTracking()
                .Include(m => m.Warehouse)
                .Where(m => m.ItemId == itemId)
                .OrderByDescending(m => m.MovementAt)
                .ThenByDescending(m => m.Id)
                .Select(m => new StockMovementItemLedgerDto
                {
                    MovementId = m.Id,
                    MovementDate = m.MovementAt,
                    WarehouseId = m.WarehouseId,
                    WarehouseName = m.Warehouse != null ? m.Warehouse.WarehouseName : string.Empty,
                    MovementType = m.MovementType,
                    Quantity = m.Quantity,
                    UnitCost = m.UnitCost,
                    ReferenceType = m.ReferenceType ?? string.Empty,
                    ReferenceId = m.ReferenceId,
                    Notes = m.Notes ?? string.Empty
                })
                .ToListAsync();
        }
    }
}