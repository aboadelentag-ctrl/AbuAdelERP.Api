using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AbuAdelERP.Api.Models;

namespace AbuAdelERP.Api.Controllers
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

        // 1. استرجاع قائمة التحويلات للفروع
        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] long? branchId)
        {
            var query = _context.BranchTransfers
                .AsNoTracking()
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
                t.DispatchedCount,
                t.DriverName,
                t.VehicleNumber,
                t.DispatcherName,
                t.ReceivedQty,
                t.ReceivedCount,
                t.ReceiverName,
                t.Status,
                t.DisputeReason,
                t.DisputeChargeTo,
                t.DisputeNotes,
                t.ResolvedBy,
                t.Notes
            });

            return Ok(result);
        }

        // 2. إرسال وتحميل شحنة من الكنترول إلى الفرع
        [HttpPost("dispatch")]
        public async Task<IActionResult> Dispatch([FromBody] BranchDispatchDto dto)
        {
            if (dto.DispatchedQty <= 0)
                return BadRequest("الكمية المحولة يجب أن تكون أكبر من صفر.");

            var item = await _context.Items.FindAsync(dto.ItemId);
            if (item == null) return NotFound("الصنف غير موجود.");

            var now = DateTime.UtcNow;
            string transferNumber = $"BTRF-{now:yyyyMMdd}-{(await _context.BranchTransfers.CountAsync() + 1):D4}";

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var transfer = new BranchTransfer
                {
                    TransferNumber = transferNumber,
                    TransferDate = DateOnly.FromDateTime(DateTime.Today),
                    SourceWarehouseId = dto.SourceWarehouseId,
                    DestinationWarehouseId = dto.DestinationWarehouseId,
                    DestinationBranchId = dto.DestinationBranchId,
                    ItemId = dto.ItemId,
                    BatchNumber = dto.BatchNumber,
                    DispatchedQty = dto.DispatchedQty,
                    DispatchedCount = dto.DispatchedCount,
                    DriverName = dto.DriverName,
                    VehicleNumber = dto.VehicleNumber,
                    DispatcherName = dto.DispatcherName,
                    Notes = dto.Notes,
                    Status = "IN_TRANSIT"
                };

                _context.BranchTransfers.Add(transfer);

                // خصم من رصيد الكنترول
                var controlBal = await _context.StockBalances
                    .FirstOrDefaultAsync(b => b.ItemId == dto.ItemId && b.WarehouseId == dto.SourceWarehouseId);

                if (controlBal != null)
                {
                    controlBal.Quantity -= dto.DispatchedQty;
                    controlBal.LastUpdatedAt = now;
                }

                // تسجيل حركة صرف تحويل
                _context.StockMovements.Add(new StockMovement
                {
                    CompanyId = 1,
                    WarehouseId = dto.SourceWarehouseId,
                    ItemId = dto.ItemId,
                    MovementType = "FG_TRANSFER_OUT",
                    Quantity = -dto.DispatchedQty,
                    UnitCost = item.BasePrice,
                    ReferenceType = "BRANCH_TRANSFER",
                    MovementAt = now,
                    CreatedAt = now,
                    Notes = $"شحن للفرع بإذن [{transferNumber}] | السائق: {dto.DriverName}"
                });

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Ok(new { success = true, message = "تم شحن البضاعة وتمريرها في الطريق للفرع بنجاح." });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, ex.Message);
            }
        }

        // 3. استلام الفرع الفعلي والترصيد
        [HttpPost("receive")]
        public async Task<IActionResult> Receive([FromBody] BranchReceiveDto dto)
        {
            var transfer = await _context.BranchTransfers.FindAsync(dto.TransferId);
            if (transfer == null) return NotFound("إذن التحويل غير موجود.");

            if (transfer.Status == "COMPLETED")
                return BadRequest("هذا الإذن تم استلامه وترصيده مسبقاً.");

            var item = await _context.Items.FindAsync(transfer.ItemId);
            var now = DateTime.UtcNow;

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                transfer.ReceivedQty = dto.ReceivedQty;
                transfer.ReceivedCount = dto.ReceivedCount;
                transfer.ReceiverName = dto.ReceiverName;
                transfer.UpdatedAt = now;

                // فحص هل توجد نسبة عجز تستوجب تصعيد نزاع؟
                if (dto.ReceivedQty < transfer.DispatchedQty)
                {
                    transfer.Status = "IN_DISPUTE";
                    transfer.DisputeReason = $"عجز استلام بالفرع بمقدار {(transfer.DispatchedQty - dto.ReceivedQty):N2} وملاحظات: {dto.Notes}";
                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    return Ok(new { success = false, inDispute = true, message = "تم إحالة الشحنة إلى لجنة فض المنازعات لوجود عجز في الاستلام." });
                }

                // الاستلام مطابق 100%: ترصيد مباشر بمخزن الفرع
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
                        CompanyId = 1,
                        WarehouseId = transfer.DestinationWarehouseId,
                        ItemId = transfer.ItemId,
                        Quantity = dto.ReceivedQty,
                        ReservedQuantity = 0,
                        LastUpdatedAt = now
                    });
                }

                _context.StockMovements.Add(new StockMovement
                {
                    CompanyId = 1,
                    WarehouseId = transfer.DestinationWarehouseId,
                    ItemId = transfer.ItemId,
                    MovementType = "FG_TRANSFER_IN",
                    Quantity = dto.ReceivedQty,
                    UnitCost = item?.BasePrice ?? 0,
                    ReferenceType = "BRANCH_TRANSFER",
                    ReferenceId = transfer.Id,
                    MovementAt = now,
                    CreatedAt = now,
                    Notes = $"استلام سليم بالفرع بإذن [{transfer.TransferNumber}] | المستلم: {dto.ReceiverName}"
                });

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Ok(new { success = true, inDispute = false, message = "تم استلام الشحنة وترصيد الكمية بمستودع الفرع بنجاح." });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, ex.Message);
            }
        }

        // 4. لجنة فض المنازعات بين الكنترول والفرع
        [HttpPost("resolve-dispute")]
        public async Task<IActionResult> ResolveDispute([FromBody] ResolveBranchDisputeDto dto)
        {
            var transfer = await _context.BranchTransfers.FindAsync(dto.TransferId);
            if (transfer == null) return NotFound("الإذن غير موجود.");

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

                // 1. ترصيد الكمية الفعلية المقبولة في مخزن الفرع
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
                        CompanyId = 1,
                        WarehouseId = transfer.DestinationWarehouseId,
                        ItemId = transfer.ItemId,
                        Quantity = dto.FinalAcceptedQty,
                        ReservedQuantity = 0,
                        LastUpdatedAt = now
                    });
                }

                _context.StockMovements.Add(new StockMovement
                {
                    CompanyId = 1,
                    WarehouseId = transfer.DestinationWarehouseId,
                    ItemId = transfer.ItemId,
                    MovementType = "FG_TRANSFER_IN",
                    Quantity = dto.FinalAcceptedQty,
                    UnitCost = item?.BasePrice ?? 0,
                    ReferenceType = "BRANCH_TRANSFER",
                    ReferenceId = transfer.Id,
                    MovementAt = now,
                    CreatedAt = now,
                    Notes = $"استلام بقرار لجنة فض المنازعات بإذن [{transfer.TransferNumber}]"
                });

                // 2. إثبات العجز كتسوية هدر / عهدة
                decimal deficit = transfer.DispatchedQty - dto.FinalAcceptedQty;
                if (deficit > 0)
                {
                    _context.StockMovements.Add(new StockMovement
                    {
                        CompanyId = 1,
                        WarehouseId = transfer.SourceWarehouseId,
                        ItemId = transfer.ItemId,
                        MovementType = "FG_TRANSIT_DEFICIT",
                        Quantity = -deficit,
                        UnitCost = item?.BasePrice ?? 0,
                        ReferenceType = "BRANCH_TRANSFER_DEFICIT",
                        ReferenceId = transfer.Id,
                        MovementAt = now,
                        CreatedAt = now,
                        Notes = $"عجز نقل محمل على [{dto.ChargeTo}] | قرار اللجنة: {dto.Notes}"
                    });
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Ok(new { success = true, message = "تم فض النزاع وترصيد الكمية للفرع وتسوية العجز بنجاح." });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, ex.Message);
            }
        }
    }

    public class BranchDispatchDto
    {
        public long SourceWarehouseId { get; set; }
        public long DestinationWarehouseId { get; set; }
        public long DestinationBranchId { get; set; }
        public long ItemId { get; set; }
        public string? BatchNumber { get; set; }
        public decimal DispatchedQty { get; set; }
        public int DispatchedCount { get; set; }
        public string? DriverName { get; set; }
        public string? VehicleNumber { get; set; }
        public string? DispatcherName { get; set; }
        public string? Notes { get; set; }
    }

    public class BranchReceiveDto
    {
        public long TransferId { get; set; }
        public decimal ReceivedQty { get; set; }
        public int ReceivedCount { get; set; }
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