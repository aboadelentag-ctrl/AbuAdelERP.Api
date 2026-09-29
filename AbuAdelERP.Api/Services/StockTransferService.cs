using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using AbuAdelERP.Api.Models;
using AbuAdelERP.Api.DTOs;

namespace AbuAdelERP.Api.Services
{
    public interface IStockTransferService
    {
        Task<List<StockTransferDetailDto>> GetAllTransfersAsync(string? status = null);
        Task<StockTransferDetailDto?> GetTransferByIdAsync(long id);
        Task<StockTransferDetailDto> CreateAndDispatchTransferAsync(CreateStockTransferDto dto);
        Task<bool> ReceiveTransferLineAsync(ConfirmTransferLineReceiveDto dto);
        Task<bool> ResolveTransferDisputeAsync(ResolveTransferDisputeDto dto);
        Task<AvailableStockInfoDto> GetAvailableBatchesAsync(long itemId, long warehouseId);
    }

    public class StockTransferService : IStockTransferService
    {
        private readonly AppDbContext _context;

        public StockTransferService(AppDbContext context)
        {
            _context = context;
        }

        // 1. جلب كافة أذون التحويل بأمان مع تجنب أخطاء الـ Null
        public async Task<List<StockTransferDetailDto>> GetAllTransfersAsync(string? status = null)
        {
            var query = _context.StockTransfers
                .Include(t => t.FromWarehouse)
                .Include(t => t.ToWarehouse)
                .Include(t => t.StockTransferLines).ThenInclude(l => l.Item)
                .AsNoTracking();

            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(t => t.Status == status);
            }

            var list = await query.OrderByDescending(t => t.CreatedAt).ToListAsync();
            return list.Select(MapToDetailDto).ToList();
        }

        // 2. جلب تفاصيل إذن تحويل محدد
        public async Task<StockTransferDetailDto?> GetTransferByIdAsync(long id)
        {
            var t = await _context.StockTransfers
                .Include(x => x.FromWarehouse)
                .Include(x => x.ToWarehouse)
                .Include(x => x.StockTransferLines).ThenInclude(l => l.Item)
                .FirstOrDefaultAsync(x => x.Id == id);

            return t == null ? null : MapToDetailDto(t);
        }

        // 3. الاستعلام عن الرصيد المتاح واللوطات الفعلية (FEFO) لمنع التلاعب
        public async Task<AvailableStockInfoDto> GetAvailableBatchesAsync(long itemId, long warehouseId)
        {
            var balance = await _context.StockBalances
                .AsNoTracking()
                .FirstOrDefaultAsync(b => b.ItemId == itemId && b.WarehouseId == warehouseId);

            decimal totalQty = balance?.AvailableQuantity ?? 0m;

            var receipts = await _context.StockMovements
                .AsNoTracking()
                .Where(m => m.ItemId == itemId && m.WarehouseId == warehouseId &&
                           (m.MovementType == "PURCHASE_RECEIPT" || m.MovementType == "TRANSFER_IN" || m.MovementType == "PRODUCTION_RECEIPT"))
                .OrderBy(m => m.MovementAt)
                .ToListAsync();

            var batches = new List<AvailableBatchItemDto>();
            foreach (var r in receipts)
            {
                if (string.IsNullOrWhiteSpace(r.Notes)) continue;
                string batch = ExtractValue(r.Notes, "تشغيلة:", "|");
                string expStr = ExtractValue(r.Notes, "صلاحية:", "|");
                DateTime? expDate = null;
                if (DateTime.TryParse(expStr, out DateTime p)) expDate = p;

                if (!string.IsNullOrWhiteSpace(batch))
                {
                    if (!batches.Any(b => b.BatchNumber.Equals(batch, StringComparison.OrdinalIgnoreCase)))
                    {
                        batches.Add(new AvailableBatchItemDto
                        {
                            BatchNumber = batch,
                            ExpiryDate = expDate,
                            AvailableQty = Convert.ToDecimal(r.Quantity),
                            UnitCost = Convert.ToDecimal(r.UnitCost)
                        });
                    }
                }
            }

            if (!batches.Any() && totalQty > 0)
            {
                batches.Add(new AvailableBatchItemDto
                {
                    BatchNumber = $"LOT-WH{warehouseId}-{DateTime.Now:yyyyMMdd}",
                    ExpiryDate = DateTime.Today.AddYears(1),
                    AvailableQty = totalQty,
                    UnitCost = 0
                });
            }

            return new AvailableStockInfoDto
            {
                TotalAvailableQty = totalQty,
                Batches = batches.OrderBy(b => b.ExpiryDate ?? DateTime.MaxValue).ToList()
            };
        }

        // 4. إنشاء إذن التحويل وصرفه فورياً من المخزن الراسل (In-Transit)
        public async Task<StockTransferDetailDto> CreateAndDispatchTransferAsync(CreateStockTransferDto dto)
        {
            if (dto.FromWarehouseId <= 0 || dto.ToWarehouseId <= 0 || dto.FromWarehouseId == dto.ToWarehouseId)
                throw new ArgumentException("يرجى اختيار مخزن راسل ومخزن مستقبل مختلفين.");

            if (dto.Lines == null || !dto.Lines.Any(l => l.ItemId > 0 && l.DispatchedQty > 0))
                throw new ArgumentException("يجب إضافة صنف واحد على الأقل بكمية صالحة للتحويل.");

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                DateTime now = DateTime.UtcNow;
                string trNumber = $"TR-{now:yyyyMMdd}-{new Random().Next(1000, 9999)}";

                var transfer = new StockTransfer
                {
                    CompanyId = 1,
                    TransferNumber = trNumber,
                    TransferDate = DateOnly.FromDateTime(DateTime.Today),
                    FromWarehouseId = dto.FromWarehouseId,
                    ToWarehouseId = dto.ToWarehouseId,
                    Status = "IN_TRANSIT",
                    DriverName = dto.DriverName?.Trim(),
                    VehicleNumber = dto.VehicleNumber?.Trim(),
                    Notes = dto.Notes,
                    CreatedBy = dto.DispatchedBy,
                    CreatedAt = now,
                    DispatchedAt = now,
                    UpdatedAt = now
                };

                _context.StockTransfers.Add(transfer);
                await _context.SaveChangesAsync();

                foreach (var lineDto in dto.Lines.Where(l => l.ItemId > 0 && l.DispatchedQty > 0))
                {
                    var line = new StockTransferLine
                    {
                        TransferId = transfer.Id,
                        ItemId = lineDto.ItemId,
                        Quantity = lineDto.DispatchedQty,
                        ReceivedQuantity = 0,
                        BatchNumber = !string.IsNullOrWhiteSpace(lineDto.BatchNumber) ? lineDto.BatchNumber : "لوط افتراضي",
                        ExpiryDate = lineDto.ExpiryDate.HasValue ? DateTime.SpecifyKind(lineDto.ExpiryDate.Value.Date, DateTimeKind.Utc) : null,
                        UnitCost = lineDto.UnitCost,
                        LineStatus = "PENDING",
                        LossQty = 0,
                        CreatedAt = now
                    };
                    _context.StockTransferLines.Add(line);

                    // خصم الرصيد من المخزن الراسل
                    var sourceBalance = await _context.StockBalances
                        .FirstOrDefaultAsync(b => b.ItemId == lineDto.ItemId && b.WarehouseId == dto.FromWarehouseId);

                    if (sourceBalance != null)
                    {
                        sourceBalance.Quantity -= lineDto.DispatchedQty;
                        sourceBalance.LastUpdatedAt = now;
                    }
                    else
                    {
                        _context.StockBalances.Add(new StockBalance
                        {
                            CompanyId = 1,
                            ItemId = lineDto.ItemId,
                            WarehouseId = dto.FromWarehouseId,
                            Quantity = -lineDto.DispatchedQty,
                            ReservedQuantity = 0,
                            LastUpdatedAt = now
                        });
                    }

                    // تسجيل قيد خروج البضاعة
                    _context.StockMovements.Add(new StockMovement
                    {
                        CompanyId = 1,
                        ItemId = lineDto.ItemId,
                        WarehouseId = dto.FromWarehouseId,
                        MovementType = "TRANSFER_OUT",
                        Quantity = lineDto.DispatchedQty,
                        UnitCost = lineDto.UnitCost,
                        ReferenceType = "STOCK_TRANSFER",
                        ReferenceId = transfer.Id,
                        MovementAt = now,
                        Notes = $"تحويل صادر إلى إذن رقم: {trNumber} | تشغيلة: {line.BatchNumber} | صلاحية: {line.ExpiryDate:yyyy-MM-dd} | السائق: {dto.DriverName ?? "نقل مباشر"}"
                    });
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return (await GetTransferByIdAsync(transfer.Id))!;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        // 5. تأكيد استلام سطر التحويل في المخزن المستقبل صنفاً بصنف
        public async Task<bool> ReceiveTransferLineAsync(ConfirmTransferLineReceiveDto dto)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var line = await _context.StockTransferLines
                    .Include(l => l.Transfer)
                    .Include(l => l.Item)
                    .FirstOrDefaultAsync(l => l.Id == dto.LineId);

                if (line == null) return false;

                line.ReceivedQuantity = dto.ReceivedQty;

                if (dto.IsAccepted && dto.ReceivedQty == line.Quantity)
                {
                    line.LineStatus = "ACCEPTED";
                    line.DiscrepancyReason = null;
                }
                else
                {
                    line.LineStatus = "DISPUTED";
                    line.DiscrepancyReason = dto.DiscrepancyReason ?? "عجز أو اختلاف في كمية التحويل المستلمة";
                    line.Transfer!.Status = "DISPUTED";
                }

                line.Transfer!.ReceivedBy = dto.ReceivedBy;

                var allLines = await _context.StockTransferLines
                    .Where(l => l.TransferId == line.TransferId)
                    .ToListAsync();

                if (allLines.All(l => l.LineStatus == "ACCEPTED"))
                {
                    await FinalizeTransferReceiptAsync(line.Transfer!);
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                return true;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        // 6. اعتماد قرار لجنة فض المنازعات وتسوية الفروق وعجز النقل
        public async Task<bool> ResolveTransferDisputeAsync(ResolveTransferDisputeDto dto)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var line = await _context.StockTransferLines
                    .Include(l => l.Transfer)
                    .FirstOrDefaultAsync(l => l.Id == dto.LineId);

                if (line == null) return false;

                line.ReceivedQuantity = dto.FinalApprovedQty;
                line.LossQty = dto.LossQty;
                line.ResolutionNotes = dto.ResolutionNotes;
                line.LineStatus = "ACCEPTED";

                var allLines = await _context.StockTransferLines
                    .Where(l => l.TransferId == line.TransferId)
                    .ToListAsync();

                if (allLines.All(l => l.LineStatus == "ACCEPTED"))
                {
                    var transfer = await _context.StockTransfers.FindAsync(line.TransferId);
                    if (transfer != null)
                    {
                        await FinalizeTransferReceiptAsync(transfer);
                    }
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                return true;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        private async Task FinalizeTransferReceiptAsync(StockTransfer transfer)
        {
            transfer.Status = "COMPLETED";
            transfer.CompletedAt = DateTime.UtcNow;

            var lines = await _context.StockTransferLines
                .Include(l => l.Item)
                .Where(l => l.TransferId == transfer.Id)
                .ToListAsync();

            DateTime now = DateTime.UtcNow;

            foreach (var line in lines)
            {
                if (line.ReceivedQuantity > 0)
                {
                    var targetBalance = await _context.StockBalances
                        .FirstOrDefaultAsync(b => b.ItemId == line.ItemId && b.WarehouseId == transfer.ToWarehouseId);

                    if (targetBalance != null)
                    {
                        targetBalance.Quantity += line.ReceivedQuantity;
                        targetBalance.LastUpdatedAt = now;
                    }
                    else
                    {
                        _context.StockBalances.Add(new StockBalance
                        {
                            CompanyId = 1,
                            ItemId = line.ItemId,
                            WarehouseId = transfer.ToWarehouseId,
                            Quantity = line.ReceivedQuantity,
                            ReservedQuantity = 0,
                            LastUpdatedAt = now
                        });
                    }

                    _context.StockMovements.Add(new StockMovement
                    {
                        CompanyId = 1,
                        ItemId = line.ItemId,
                        WarehouseId = transfer.ToWarehouseId,
                        MovementType = "TRANSFER_IN",
                        Quantity = line.ReceivedQuantity,
                        UnitCost = line.UnitCost,
                        ReferenceType = "STOCK_TRANSFER",
                        ReferenceId = transfer.Id,
                        MovementAt = now,
                        Notes = $"استلام تحويل وارد من إذن: {transfer.TransferNumber} | تشغيلة: {line.BatchNumber} | صلاحية: {line.ExpiryDate:yyyy-MM-dd} | مستلم: {transfer.ReceivedBy ?? "أمين المخزن"}"
                    });
                }

                if (line.LossQty > 0)
                {
                    _context.StockMovements.Add(new StockMovement
                    {
                        CompanyId = 1,
                        ItemId = line.ItemId,
                        WarehouseId = transfer.FromWarehouseId,
                        MovementType = "TRANSFER_LOSS",
                        Quantity = line.LossQty,
                        UnitCost = line.UnitCost,
                        ReferenceType = "STOCK_TRANSFER",
                        ReferenceId = transfer.Id,
                        MovementAt = now,
                        Notes = $"إثبات هالك نقل / عجز شحنة إذن رقم: {transfer.TransferNumber} | قرار لجنة التسوية: {line.ResolutionNotes}"
                    });
                }
            }
        }

        private static StockTransferDetailDto MapToDetailDto(StockTransfer t)
        {
            return new StockTransferDetailDto
            {
                Id = t.Id,
                TransferNumber = t.TransferNumber,
                FromWarehouseId = t.FromWarehouseId,
                FromWarehouseName = t.FromWarehouse?.WarehouseName ?? "مخزن غير محدد",
                ToWarehouseId = t.ToWarehouseId,
                ToWarehouseName = t.ToWarehouse?.WarehouseName ?? "مخزن غير محدد",
                Status = t.Status,
                DriverName = t.DriverName,
                VehicleNumber = t.VehicleNumber,
                Notes = t.Notes,
                CreatedBy = t.CreatedBy,
                ReceivedBy = t.ReceivedBy,
                CreatedAt = t.CreatedAt ?? DateTime.UtcNow,
                DispatchedAt = t.DispatchedAt,
                CompletedAt = t.CompletedAt,
                Lines = t.StockTransferLines.Select(l => new StockTransferLineDetailDto
                {
                    Id = l.Id,
                    ItemId = l.ItemId,
                    ItemCode = l.Item?.ItemCode ?? "",
                    ItemName = l.Item?.ItemName ?? "صنف",
                    Unit = l.Item?.Unit ?? "",
                    DispatchedQty = l.Quantity,
                    ReceivedQty = l.ReceivedQuantity,
                    BatchNumber = l.BatchNumber,
                    ExpiryDate = l.ExpiryDate,
                    UnitCost = l.UnitCost,
                    LineStatus = l.LineStatus,
                    DiscrepancyReason = l.DiscrepancyReason,
                    ResolutionNotes = l.ResolutionNotes,
                    LossQty = l.LossQty
                }).ToList()
            };
        }

        private static string ExtractValue(string text, string key, string separator)
        {
            int startIdx = text.IndexOf(key);
            if (startIdx == -1) return "";
            startIdx += key.Length;
            int endIdx = text.IndexOf(separator, startIdx);
            if (endIdx == -1) endIdx = text.Length;
            return text.Substring(startIdx, endIdx - startIdx).Trim();
        }
    }
}