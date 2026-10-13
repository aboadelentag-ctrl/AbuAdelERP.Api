using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using KayanERP.Api.Models;

namespace KayanERP.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BranchTransfersController : ControllerBase
    {
        private readonly AppDbContext _context;

        public BranchTransfersController(AppDbContext context)
        {
            _context = context;
        }

        // 1. جلب البيانات المرجعية مع تصفية صارمة للمنتجات التامة فقط (استبعاد الخامات والأدوات)
        [HttpGet("lookups")]
        public async Task<IActionResult> GetLookups([FromQuery] long companyId = 1)
        {
            var branches = await _context.Branches
                .AsNoTracking()
                .Where(b => b.CompanyId == companyId || b.CompanyId == null)
                .Select(b => new { b.Id, b.BranchName, b.BranchCode })
                .ToListAsync();

            var warehouses = await _context.Warehouses
                .AsNoTracking()
                .Where(w => w.CompanyId == companyId || w.CompanyId == null)
                .Select(w => new { w.Id, w.WarehouseName, w.BranchId })
                .ToListAsync();

            // جلب الأصناف مع استبعاد الخامات ومستلزمات التعبئة (فقط منتجات تامة)
            var allItems = await _context.Items
                .AsNoTracking()
                .Where(i => (i.CompanyId == companyId || i.CompanyId == null) && i.Active)
                .Select(i => new { i.Id, i.ItemName, i.ItemCode, Unit = i.Unit ?? "قطعة" })
                .ToListAsync();

            var finishedItems = allItems
                .Where(i => !i.ItemCode.StartsWith("10") &&
                            !i.ItemName.Contains("سكر") &&
                            !i.ItemName.Contains("دقيق") &&
                            !i.ItemName.Contains("سميد") &&
                            !i.ItemName.Contains("سبت") &&
                            !i.ItemName.Contains("صينية ألمنيوم") &&
                            !i.ItemName.Contains("عسل"))
                .OrderBy(i => i.ItemName)
                .ToList();

            return Ok(new { branches, warehouses, items = finishedItems });
        }

        // 2. سحب آخر رقم تشغيلة (لوط) للمنتج التام تلقائياً من التشغيل
        [HttpGet("item-batch/{itemId}")]
        public async Task<IActionResult> GetItemLatestBatch(long itemId)
        {
            var lastBatch = await _context.BranchTransfers
                .Where(t => t.ItemId == itemId && !string.IsNullOrEmpty(t.BatchNumber))
                .OrderByDescending(t => t.Id)
                .Select(t => t.BatchNumber)
                .FirstOrDefaultAsync();

            if (string.IsNullOrEmpty(lastBatch))
            {
                lastBatch = $"LOT-{DateTime.Today:yyyyMMdd}-{itemId:D2}";
            }

            return Ok(new { batchNumber = lastBatch });
        }

        // 3. استرجاع قائمة الشحنات العامة
        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] long companyId = 1, [FromQuery] long? branchId = null)
        {
            var query = _context.BranchTransfers
                .AsNoTracking()
                .Where(t => t.CompanyId == companyId)
                .Include(t => t.Item)
                .Include(t => t.SourceWarehouse)
                .Include(t => t.DestinationWarehouse)
                .Include(t => t.DestinationBranch)
                .AsQueryable();

            if (branchId.HasValue && branchId.Value > 0)
            {
                query = query.Where(t => t.DestinationBranchId == branchId.Value);
            }

            var list = await query.OrderByDescending(t => t.Id).ToListAsync();

            var result = list.Select(t => new
            {
                t.Id,
                t.TransferNumber,
                t.TransferDate,
                t.SourceWarehouseId,
                SourceWarehouseName = t.SourceWarehouse?.WarehouseName ?? "الكنترول الرئيسي",
                t.DestinationWarehouseId,
                DestinationWarehouseName = t.DestinationWarehouse?.WarehouseName ?? "مستودع الفرع",
                t.DestinationBranchId,
                DestinationBranchName = t.DestinationBranch?.BranchName ?? "الفرع المستلم",
                t.ItemId,
                ItemName = t.Item?.ItemName ?? "منتج تام",
                ItemCode = t.Item?.ItemCode ?? "",
                Unit = t.Item?.Unit ?? "قطعة",
                t.BatchNumber,
                t.DispatchedQty,
                t.ContainerType,
                t.InnerCount,
                t.CrateCount,
                t.DispatchedCount,
                t.DriverName,
                t.VehicleNumber,
                t.DispatcherName,
                t.ReceivedQty,
                t.ReceivedInnerCount,
                t.ReceivedCrateCount,
                t.ReceivedCount,
                t.ReceiverName,
                t.Status,
                t.DisputeReason,
                t.DisputeChargeTo,
                t.DisputeNotes,
                t.ResolvedBy,
                t.Notes,
                t.CreatedAt
            });

            return Ok(result);
        }

        // 4. شحن مانيفست متعدد الأصناف مع تتبع السرافيس والأسبات وحل مشكلة الحفظ
        [HttpPost("dispatch-multi")]
        public async Task<IActionResult> DispatchMulti([FromBody] MultiItemBranchDispatchDto dto)
        {
            if (dto.Items == null || !dto.Items.Any(i => i.ItemId > 0 && i.DispatchedQty > 0))
                return BadRequest("يجب إضافة صنف واحد على الأقل بكمية صالحة للشحن.");

            long cId = dto.CompanyId > 0 ? dto.CompanyId : 1;
            var now = DateTime.UtcNow;

            // توليد رقم المانيفست الموحد
            int nextSeq = await _context.BranchTransfers.Select(t => t.TransferNumber).Distinct().CountAsync() + 1;
            string manifestBase = $"BTRF-{now:yyyyMMdd}-{nextSeq:D4}";

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                int lineIndex = 1;
                foreach (var line in dto.Items.Where(i => i.ItemId > 0 && i.DispatchedQty > 0))
                {
                    var item = await _context.Items.FindAsync(line.ItemId);
                    if (item == null) continue;

                    string lineTransferNumber = $"{manifestBase}-{lineIndex:D2}";

                    var transfer = new BranchTransfer
                    {
                        CompanyId = cId,
                        TransferNumber = lineTransferNumber,
                        TransferDate = DateOnly.FromDateTime(DateTime.Today),
                        SourceWarehouseId = dto.SourceWarehouseId,
                        DestinationWarehouseId = dto.DestinationWarehouseId,
                        DestinationBranchId = dto.DestinationBranchId,
                        ItemId = line.ItemId,
                        BatchNumber = line.BatchNumber,
                        DispatchedQty = line.DispatchedQty,
                        ContainerType = line.ContainerType ?? "سرفيس",
                        InnerCount = line.InnerCount,
                        CrateCount = line.CrateCount,
                        DispatchedCount = line.InnerCount > 0 ? line.InnerCount : 1,
                        DriverName = string.IsNullOrWhiteSpace(dto.DriverName) ? "سائق التوزيع" : dto.DriverName.Trim(),
                        VehicleNumber = string.IsNullOrWhiteSpace(dto.VehicleNumber) ? "سيارة نقل" : dto.VehicleNumber.Trim(),
                        DispatcherName = dto.DispatcherName ?? "مسؤول الكنترول",
                        Notes = dto.Notes,
                        Status = "IN_TRANSIT",
                        CreatedAt = now,
                        UpdatedAt = now
                    };

                    _context.BranchTransfers.Add(transfer);
                    await _context.SaveChangesAsync();

                    // خصم رصيد الكنترول إذا كان مسجلاً دون رمي استثناء رصيد سالب
                    var controlBal = await _context.StockBalances
                        .FirstOrDefaultAsync(b => b.ItemId == line.ItemId && b.WarehouseId == dto.SourceWarehouseId);

                    if (controlBal != null)
                    {
                        controlBal.Quantity -= line.DispatchedQty;
                        controlBal.LastUpdatedAt = now;
                    }

                    // تسجيل حركة الصرف اللوجستي
                    _context.StockMovements.Add(new StockMovement
                    {
                        CompanyId = cId,
                        WarehouseId = dto.SourceWarehouseId,
                        ItemId = line.ItemId,
                        MovementType = "FG_TRANSFER_OUT",
                        Quantity = -line.DispatchedQty,
                        UnitCost = item.BasePrice,
                        ReferenceType = "BRANCH_TRANSFER",
                        MovementAt = now,
                        CreatedAt = now,
                        Notes = $"شحن للفرع بإذن [{lineTransferNumber}] | صنف: {item.ItemName} | السائق: {dto.DriverName}"
                    });

                    lineIndex++;
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Ok(new { success = true, transferNumber = manifestBase, message = "تم شحن المانيفست بالكامل بنجاح." });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, ex.Message);
            }
        }

        // 5. استلام صنف محدد داخل الشحنة مع أوعية النقل
        [HttpPost("receive-line")]
        public async Task<IActionResult> ReceiveLine([FromBody] BranchReceiveLineDto dto)
        {
            var transfer = await _context.BranchTransfers.FindAsync(dto.TransferId);
            if (transfer == null) return NotFound("البند غير موجود.");

            if (transfer.Status == "COMPLETED")
                return BadRequest("هذا البند تم استلامه مسبقاً.");

            var item = await _context.Items.FindAsync(transfer.ItemId);
            var now = DateTime.UtcNow;

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                transfer.ReceivedQty = dto.ReceivedQty;
                transfer.ReceivedInnerCount = dto.ReceivedInnerCount;
                transfer.ReceivedCrateCount = dto.ReceivedCrateCount;
                transfer.ReceivedCount = dto.ReceivedInnerCount;
                transfer.ReceiverName = dto.ReceiverName;
                transfer.UpdatedAt = now;

                if (dto.ReceivedQty < transfer.DispatchedQty)
                {
                    transfer.Status = "IN_DISPUTE";
                    transfer.DisputeReason = $"عجز استلام بمقدار {(transfer.DispatchedQty - dto.ReceivedQty):N2} | ملاحظات: {dto.Notes}";
                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    return Ok(new { success = true, inDispute = true, message = "تم إحالة الصنف للجنة فض المنازعات لوجود عجز." });
                }

                transfer.Status = "COMPLETED";

                var branchBal = await _context.StockBalances
                    .FirstOrDefaultAsync(b => b.ItemId == transfer.ItemId && b.WarehouseId == transfer.DestinationWarehouseId);

                if (branchBal != null)
                {
                    branchBal.Quantity += dto.ReceivedQty;
                    branchBal.LastUpdatedAt = now;
                }
                else
                {
                    _context.StockBalances.Add(new StockBalance
                    {
                        CompanyId = transfer.CompanyId,
                        WarehouseId = transfer.DestinationWarehouseId,
                        ItemId = transfer.ItemId,
                        Quantity = dto.ReceivedQty,
                        ReservedQuantity = 0,
                        LastUpdatedAt = now
                    });
                }

                _context.StockMovements.Add(new StockMovement
                {
                    CompanyId = transfer.CompanyId,
                    WarehouseId = transfer.DestinationWarehouseId,
                    ItemId = transfer.ItemId,
                    MovementType = "FG_TRANSFER_IN",
                    Quantity = dto.ReceivedQty,
                    UnitCost = item?.BasePrice ?? 0,
                    ReferenceType = "BRANCH_TRANSFER",
                    MovementAt = now,
                    CreatedAt = now,
                    Notes = $"استلام سليم بالفرع بإذن [{transfer.TransferNumber}] | المستلم: {dto.ReceiverName}"
                });

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Ok(new { success = true, inDispute = false, message = "تم استلام الصنف وترصيده بنجاح." });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, ex.Message);
            }
        }

        // 6. حسم نزاع صنف محدد في لجنة فض المنازعات
        [HttpPost("resolve-dispute")]
        public async Task<IActionResult> ResolveDispute([FromBody] ResolveBranchDisputeDto dto)
        {
            var transfer = await _context.BranchTransfers.FindAsync(dto.TransferId);
            if (transfer == null) return NotFound("البند غير موجود.");

            var item = await _context.Items.FindAsync(transfer.ItemId);
            var now = DateTime.UtcNow;

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                transfer.Status = "COMPLETED";
                transfer.ReceivedQty = dto.FinalAcceptedQty;
                transfer.DisputeChargeTo = dto.ChargeTo;
                transfer.DisputeNotes = dto.Notes;
                transfer.ResolvedBy = dto.ResolvedBy;
                transfer.UpdatedAt = now;

                var branchBal = await _context.StockBalances
                    .FirstOrDefaultAsync(b => b.ItemId == transfer.ItemId && b.WarehouseId == transfer.DestinationWarehouseId);

                if (branchBal != null)
                {
                    branchBal.Quantity += dto.FinalAcceptedQty;
                    branchBal.LastUpdatedAt = now;
                }
                else
                {
                    _context.StockBalances.Add(new StockBalance
                    {
                        CompanyId = transfer.CompanyId,
                        WarehouseId = transfer.DestinationWarehouseId,
                        ItemId = transfer.ItemId,
                        Quantity = dto.FinalAcceptedQty,
                        ReservedQuantity = 0,
                        LastUpdatedAt = now
                    });
                }

                _context.StockMovements.Add(new StockMovement
                {
                    CompanyId = transfer.CompanyId,
                    WarehouseId = transfer.DestinationWarehouseId,
                    ItemId = transfer.ItemId,
                    MovementType = "FG_TRANSFER_IN",
                    Quantity = dto.FinalAcceptedQty,
                    UnitCost = item?.BasePrice ?? 0,
                    ReferenceType = "BRANCH_TRANSFER",
                    MovementAt = now,
                    CreatedAt = now,
                    Notes = $"استلام بقرار لجنة فض المنازعات بإذن [{transfer.TransferNumber}]"
                });

                decimal deficit = transfer.DispatchedQty - dto.FinalAcceptedQty;
                if (deficit > 0)
                {
                    _context.StockMovements.Add(new StockMovement
                    {
                        CompanyId = transfer.CompanyId,
                        WarehouseId = transfer.SourceWarehouseId,
                        ItemId = transfer.ItemId,
                        MovementType = "FG_TRANSIT_DEFICIT",
                        Quantity = -deficit,
                        UnitCost = item?.BasePrice ?? 0,
                        ReferenceType = "BRANCH_TRANSFER_DEFICIT",
                        MovementAt = now,
                        CreatedAt = now,
                        Notes = $"عجز نقل محمل على [{dto.ChargeTo}] | قرار اللجنة: {dto.Notes}"
                    });
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Ok(new { success = true, message = "تم فض النزاع وترصيد الصنف وتسوية الهالك بنجاح." });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, ex.Message);
            }
        }
    }

    public class MultiItemBranchDispatchDto
    {
        public long CompanyId { get; set; } = 1;
        public long SourceWarehouseId { get; set; }
        public long DestinationWarehouseId { get; set; }
        public long DestinationBranchId { get; set; }
        public string? DriverName { get; set; }
        public string? VehicleNumber { get; set; }
        public string? DispatcherName { get; set; }
        public string? Notes { get; set; }
        public List<DispatchItemRowDto> Items { get; set; } = new();
    }

    public class DispatchItemRowDto
    {
        public long ItemId { get; set; }
        public string? BatchNumber { get; set; }
        public decimal DispatchedQty { get; set; }
        public string? ContainerType { get; set; } = "سرفيس";
        public int InnerCount { get; set; } = 1;
        public int CrateCount { get; set; } = 1;
    }

    public class BranchReceiveLineDto
    {
        public long TransferId { get; set; }
        public decimal ReceivedQty { get; set; }
        public int ReceivedInnerCount { get; set; } = 1;
        public int ReceivedCrateCount { get; set; } = 1;
        public string? ReceiverName { get; set; }
        public string? Notes { get; set; }
    }

    public class ResolveBranchDisputeDto
    {
        public long TransferId { get; set; }
        public decimal FinalAcceptedQty { get; set; }
        public string? ChargeTo { get; set; }
        public string? ResolvedBy { get; set; }
        public string? Notes { get; set; }
    }
}