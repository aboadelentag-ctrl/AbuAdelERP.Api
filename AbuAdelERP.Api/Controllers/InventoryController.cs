using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AbuAdelERP.Api.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AbuAdelERP.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class InventoryController : ControllerBase
    {
        private readonly AppDbContext _context;

        public InventoryController(AppDbContext context)
        {
            _context = context;
        }

        // ==============================================================
        // 1. جلب الأرصدة الحالية لجميع الأصناف موزعة على المخازن
        // ==============================================================
        [HttpGet("balances")]
        public async Task<IActionResult> GetBalances([FromQuery] long? warehouseId = null, [FromQuery] string? search = null)
        {
            try
            {
                var query = _context.StockBalances
                    .Include(b => b.Item)
                    .Include(b => b.Warehouse)
                    .AsNoTracking()
                    .AsQueryable();

                if (warehouseId.HasValue && warehouseId.Value > 0)
                    query = query.Where(b => b.WarehouseId == warehouseId.Value);

                if (!string.IsNullOrWhiteSpace(search))
                {
                    var term = search.Trim();
                    query = query.Where(b => (b.Item != null && (b.Item.ItemName.Contains(term) || b.Item.ItemCode.Contains(term))));
                }

                var list = await query
                    .OrderBy(b => b.ItemId)
                    .Select(b => new StockBalanceDetailedDto
                    {
                        Id = b.Id,
                        ItemId = b.ItemId,
                        ItemCode = b.Item != null ? b.Item.ItemCode : "",
                        ItemName = b.Item != null ? b.Item.ItemName : "صنف غير معروف",
                        ItemType = b.Item != null ? b.Item.ItemType : "خامات",
                        Unit = b.Item != null ? b.Item.Unit : "كجم",
                        WarehouseId = b.WarehouseId,
                        WarehouseName = b.Warehouse != null ? b.Warehouse.WarehouseName : "المخزن الرئيسي",
                        Quantity = b.Quantity,
                        ReservedQuantity = b.ReservedQuantity,
                        AvailableQuantity = b.Quantity - b.ReservedQuantity,
                        LastUpdatedAt = b.LastUpdatedAt
                    })
                    .ToListAsync();

                return Ok(list);
            }
            catch (Exception ex)
            {
                return BadRequest($"خطأ أثناء جلب الأرصدة: {ex.Message}");
            }
        }

        // ==============================================================
        // 2. تحديث وتعديل أرصدة الأصناف (من شيت الجرد أو الاستيراد)
        // ==============================================================
        [HttpPost("import-balances")]
        public async Task<IActionResult> ImportBalances([FromBody] List<StockBalanceImportDto> items)
        {
            if (items == null || !items.Any())
                return BadRequest("لا توجد بيانات للاستيراد.");

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var now = DateTime.UtcNow;
                int updatedCount = 0;
                int createdCount = 0;

                foreach (var dto in items)
                {
                    if (dto.ItemId <= 0 || dto.WarehouseId <= 0) continue;

                    var balance = await _context.StockBalances
                        .FirstOrDefaultAsync(b => b.WarehouseId == dto.WarehouseId && b.ItemId == dto.ItemId);

                    if (balance != null)
                    {
                        balance.Quantity = dto.Quantity;
                        balance.LastUpdatedAt = now;
                        updatedCount++;
                    }
                    else
                    {
                        _context.StockBalances.Add(new StockBalance
                        {
                            CompanyId = 1,
                            WarehouseId = dto.WarehouseId,
                            ItemId = dto.ItemId,
                            Quantity = dto.Quantity,
                            ReservedQuantity = 0,
                            LastUpdatedAt = now
                        });
                        createdCount++;
                    }

                    // إثبات حركة جرد / رصيد افتتاحي في كارت الصنف
                    _context.StockMovements.Add(new StockMovement
                    {
                        CompanyId = 1,
                        WarehouseId = dto.WarehouseId,
                        ItemId = dto.ItemId,
                        MovementType = "STOCK_ADJUSTMENT",
                        Quantity = dto.Quantity,
                        UnitCost = 0,
                        ReferenceType = "EXCEL_IMPORT",
                        Notes = $"تسوية وتحديث رصيد من ملف الإكسيل | {dto.Notes}",
                        MovementAt = now,
                        CreatedAt = now
                    });
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Ok(new { success = true, message = $"اكتمل التحديث: تم تعديل ({updatedCount}) صنف وإضافة ({createdCount}) رصيد مخزني جديد بنجاح." });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, $"فشل الاستيراد: {ex.GetBaseException().Message}");
            }
        }

        // 3. جلب بطاقة حركة الصنف (Item Ledger)
        [HttpGet("ledger/{itemId}")]
        public async Task<IActionResult> GetItemLedger(long itemId)
        {
            try
            {
                var movements = await _context.StockMovements
                    .Where(m => m.ItemId == itemId)
                    .OrderByDescending(m => m.MovementAt)
                    .Select(m => new StockMovementDto
                    {
                        Id = m.Id,
                        MovementDate = m.MovementAt,
                        MovementType = m.MovementType,
                        Quantity = m.Quantity,
                        ReferenceType = m.ReferenceType,
                        ReferenceId = m.ReferenceId,
                        Notes = m.Notes
                    })
                    .ToListAsync();

                return Ok(movements);
            }
            catch (Exception ex)
            {
                return BadRequest($"خطأ أثناء جلب حركة الصنف: {ex.Message}");
            }
        }
    }

    public class StockBalanceDetailedDto
    {
        public long Id { get; set; }
        public long ItemId { get; set; }
        public string ItemCode { get; set; } = string.Empty;
        public string ItemName { get; set; } = string.Empty;
        public string ItemType { get; set; } = string.Empty;
        public string Unit { get; set; } = "كجم";
        public long WarehouseId { get; set; }
        public string WarehouseName { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public decimal ReservedQuantity { get; set; }
        public decimal AvailableQuantity { get; set; }
        public DateTime LastUpdatedAt { get; set; }
    }

    public class StockBalanceImportDto
    {
        public long ItemId { get; set; }
        public long WarehouseId { get; set; }
        public decimal Quantity { get; set; }
        public string? Notes { get; set; }
    }

    public class StockMovementDto
    {
        public long Id { get; set; }
        public DateTime MovementDate { get; set; }
        public string MovementType { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public string ReferenceType { get; set; } = string.Empty;
        public long? ReferenceId { get; set; }
        public string? Notes { get; set; }
    }
}