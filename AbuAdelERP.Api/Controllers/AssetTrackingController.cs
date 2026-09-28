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
    public class AssetTrackingController : ControllerBase
    {
        private readonly AppDbContext _context;

        public AssetTrackingController(AppDbContext context)
        {
            _context = context;
        }

        // ==============================================================
        // 1. جلب قائمة بجميع الجهات (عملاء، أقسام، مخازن)
        // ==============================================================
        [HttpGet("parties")]
        public async Task<IActionResult> GetAllParties()
        {
            try
            {
                var customers = await _context.Customers
                    .Select(c => new PartyDto { PartyId = c.Id, PartyName = c.CustomerName, PartyType = "CUSTOMER" })
                    .ToListAsync();

                var departments = await _context.Departments
                    .Select(d => new PartyDto { PartyId = d.Id, PartyName = d.DepartmentName, PartyType = "DEPARTMENT" })
                    .ToListAsync();

                var warehouses = await _context.Warehouses
                    .Select(w => new PartyDto { PartyId = w.Id, PartyName = w.WarehouseName, PartyType = "WAREHOUSE" })
                    .ToListAsync();

                var allParties = new List<PartyDto>
                {
                    new PartyDto { PartyId = 0, PartyName = "النظام / رصيد افتتاحي", PartyType = "SYSTEM" }
                };

                allParties.AddRange(customers);
                allParties.AddRange(departments);
                allParties.AddRange(warehouses);

                return Ok(allParties);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"خطأ في جلب بيانات الجهات: {ex.GetBaseException().Message}");
            }
        }

        // ==============================================================
        // 2. جلب الأرصدة الحالية لجميع العهد والأصول
        // ==============================================================
        [HttpGet("balances")]
        public async Task<IActionResult> GetBalances()
        {
            try
            {
                var balances = await _context.AssetBalances
                    .Include(b => b.Item)
                    .AsNoTracking()
                    .Select(b => new AssetBalanceDto
                    {
                        Id = b.Id,
                        ItemId = b.ItemId,
                        ItemCode = b.Item != null ? b.Item.ItemCode : "",
                        ItemName = b.Item != null ? b.Item.ItemName : "غير معروف",
                        PartyType = b.PartyType,
                        PartyId = b.PartyId,
                        Quantity = b.Quantity,
                        LastUpdatedAt = b.LastUpdatedAt
                    })
                    .Where(b => b.Quantity != 0)
                    .OrderByDescending(b => b.Quantity)
                    .ToListAsync();

                return Ok(balances);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"خطأ في جلب الأرصدة: {ex.GetBaseException().Message}");
            }
        }

        // ==============================================================
        // 3. تسجيل حركة أصل جديدة وتحديث الأرصدة آلياً
        // ==============================================================
        [HttpPost("move")]
        public async Task<IActionResult> RecordMovement([FromBody] AssetMovementDto dto)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                long companyId = dto.CompanyId > 0 ? dto.CompanyId : (await _context.Companies.FirstOrDefaultAsync())?.Id ?? 1;

                DateTime safeMovementDate = DateTime.SpecifyKind(dto.MovementDate.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc);

                var movement = new AssetMovement
                {
                    CompanyId = companyId,
                    MovementDate = safeMovementDate,
                    ItemId = dto.ItemId,
                    FromPartyType = dto.FromPartyType,
                    FromPartyId = dto.FromPartyId,
                    ToPartyType = dto.ToPartyType,
                    ToPartyId = dto.ToPartyId,
                    Quantity = dto.Quantity,
                    MovementType = dto.MovementType,
                    Notes = dto.Notes,
                    CreatedAt = DateTime.UtcNow
                };

                _context.AssetMovements.Add(movement);

                // خصم الرصيد من الجهة المُسَلِّمة
                if (dto.FromPartyType != "SYSTEM")
                {
                    var fromBalance = await _context.AssetBalances
                        .FirstOrDefaultAsync(b => b.CompanyId == companyId && b.ItemId == dto.ItemId && b.PartyType == dto.FromPartyType && b.PartyId == dto.FromPartyId);

                    if (fromBalance != null)
                    {
                        fromBalance.Quantity -= dto.Quantity;
                        fromBalance.LastUpdatedAt = DateTime.UtcNow;
                    }
                    else
                    {
                        _context.AssetBalances.Add(new AssetBalance
                        {
                            CompanyId = companyId,
                            ItemId = dto.ItemId,
                            PartyType = dto.FromPartyType,
                            PartyId = dto.FromPartyId,
                            Quantity = -dto.Quantity,
                            LastUpdatedAt = DateTime.UtcNow
                        });
                    }
                }

                // إضافة الرصيد إلى الجهة المُستَلِمة
                if (dto.ToPartyType != "SYSTEM")
                {
                    var toBalance = await _context.AssetBalances
                        .FirstOrDefaultAsync(b => b.CompanyId == companyId && b.ItemId == dto.ItemId && b.PartyType == dto.ToPartyType && b.PartyId == dto.ToPartyId);

                    if (toBalance != null)
                    {
                        toBalance.Quantity += dto.Quantity;
                        toBalance.LastUpdatedAt = DateTime.UtcNow;
                    }
                    else
                    {
                        _context.AssetBalances.Add(new AssetBalance
                        {
                            CompanyId = companyId,
                            ItemId = dto.ItemId,
                            PartyType = dto.ToPartyType,
                            PartyId = dto.ToPartyId,
                            Quantity = dto.Quantity,
                            LastUpdatedAt = DateTime.UtcNow
                        });
                    }
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Ok(new { Message = "تم تسجيل الحركة وتحديث الأرصدة بنجاح", MovementId = movement.Id });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                var errorMsg = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                return BadRequest($"❌ خطأ في السيرفر أثناء تسجيل الحركة: {errorMsg}");
            }
        }

        // ==============================================================
        // 4. استيراد وتحديث أرصدة العهد من إكسيل دفعة واحدة
        // ==============================================================
        [HttpPost("import-balances")]
        public async Task<IActionResult> ImportBalances([FromBody] List<AssetBalanceImportDto> items)
        {
            if (items == null || !items.Any())
                return BadRequest("لا توجد بيانات للاستيراد.");

            var company = await _context.Companies.FirstOrDefaultAsync();
            long companyId = company?.Id ?? 1;

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var now = DateTime.UtcNow;
                int updatedCount = 0;
                int createdCount = 0;

                foreach (var dto in items)
                {
                    if (dto.ItemId <= 0 || string.IsNullOrEmpty(dto.PartyType) || dto.PartyId < 0) continue;

                    var balance = await _context.AssetBalances
                        .FirstOrDefaultAsync(b => b.CompanyId == companyId && b.ItemId == dto.ItemId && b.PartyType == dto.PartyType && b.PartyId == dto.PartyId);

                    if (balance != null)
                    {
                        balance.Quantity = dto.Quantity;
                        balance.LastUpdatedAt = now;
                        updatedCount++;
                    }
                    else
                    {
                        _context.AssetBalances.Add(new AssetBalance
                        {
                            CompanyId = companyId,
                            ItemId = dto.ItemId,
                            PartyType = dto.PartyType,
                            PartyId = dto.PartyId,
                            Quantity = dto.Quantity,
                            LastUpdatedAt = now
                        });
                        createdCount++;
                    }

                    // إثبات حركة تسوية عهدة في سجل الحركات
                    _context.AssetMovements.Add(new AssetMovement
                    {
                        CompanyId = companyId,
                        MovementDate = now,
                        ItemId = dto.ItemId,
                        FromPartyType = "SYSTEM",
                        FromPartyId = 0,
                        ToPartyType = dto.PartyType,
                        ToPartyId = dto.PartyId,
                        Quantity = dto.Quantity,
                        MovementType = "ADJUSTMENT",
                        Notes = $"تسوية وتحديث رصيد عهدة من ملف إكسيل | {dto.Notes}",
                        CreatedAt = now
                    });
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Ok(new
                {
                    success = true,
                    createdCount = createdCount,
                    updatedCount = updatedCount,
                    message = $"اكتمل التحديث بنجاح: تم تعديل ({updatedCount}) رصيد وإضافة ({createdCount}) رصيد عهدة جديد."
                });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, $"فشل حفظ الأرصدة: {ex.GetBaseException().Message}");
            }
        }
    }

    public class PartyDto
    {
        public long PartyId { get; set; }
        public string PartyName { get; set; } = string.Empty;
        public string PartyType { get; set; } = string.Empty;
    }

    public class AssetBalanceDto
    {
        public long Id { get; set; }
        public long ItemId { get; set; }
        public string ItemCode { get; set; } = string.Empty;
        public string ItemName { get; set; } = string.Empty;
        public string PartyType { get; set; } = string.Empty;
        public long PartyId { get; set; }
        public decimal Quantity { get; set; }
        public DateTime LastUpdatedAt { get; set; }
        public string PartyNameResolved { get; set; } = string.Empty;
    }

    public class AssetMovementDto
    {
        public long CompanyId { get; set; }
        public DateOnly MovementDate { get; set; }
        public long ItemId { get; set; }
        public string FromPartyType { get; set; } = string.Empty;
        public long FromPartyId { get; set; }
        public string ToPartyType { get; set; } = string.Empty;
        public long ToPartyId { get; set; }
        public decimal Quantity { get; set; }
        public string MovementType { get; set; } = string.Empty;
        public string? Notes { get; set; }
    }

    public class AssetBalanceImportDto
    {
        public long ItemId { get; set; }
        public string PartyType { get; set; } = string.Empty;
        public long PartyId { get; set; }
        public decimal Quantity { get; set; }
        public string? Notes { get; set; }
    }
}