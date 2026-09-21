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

        // 1. جلب الأرصدة الحالية لجميع الأصناف (نظرة عامة للإدارة)
        [HttpGet("balances")]
        public async Task<IActionResult> GetBalances()
        {
            try
            {
                var balances = await _context.StockBalances
                    .Include(b => b.Item)
                    .Select(b => new StockBalanceDto
                    {
                        Id = b.Id,
                        ItemId = b.ItemId,
                        ItemName = b.Item != null ? b.Item.ItemName : "صنف غير معروف",
                        WarehouseId = b.WarehouseId,
                        Quantity = b.Quantity,
                        LastUpdatedAt = b.LastUpdatedAt
                    })
                    .OrderByDescending(b => b.Quantity) // عرض الكميات الأكبر أولاً
                    .ToListAsync();

                return Ok(balances);
            }
            catch (Exception ex)
            {
                return BadRequest($"خطأ أثناء جلب الأرصدة: {ex.Message}");
            }
        }

        // 2. جلب بطاقة حركة الصنف (Item Ledger) مفصلة بالتاريخ والوقت
        [HttpGet("ledger/{itemId}")]
        public async Task<IActionResult> GetItemLedger(long itemId)
        {
            try
            {
                var movements = await _context.StockMovements
                    .Where(m => m.ItemId == itemId)
                    .OrderByDescending(m => m.MovementAt) // أحدث الحركات أولاً
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

                if (!movements.Any())
                    return NotFound("لا توجد حركات مسجلة لهذا الصنف.");

                return Ok(movements);
            }
            catch (Exception ex)
            {
                return BadRequest($"خطأ أثناء جلب حركة الصنف: {ex.Message}");
            }
        }
    }

    // ==========================================
    // DTO Classes
    // ==========================================
    public class StockBalanceDto
    {
        public long Id { get; set; }
        public long ItemId { get; set; }
        public string ItemName { get; set; } = string.Empty;
        public long WarehouseId { get; set; }
        public decimal Quantity { get; set; }
        public DateTime LastUpdatedAt { get; set; }
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