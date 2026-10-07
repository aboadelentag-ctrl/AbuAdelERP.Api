using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using AbuAdelERP.Api.Models;
using AbuAdelERP.Api.DTOs;

namespace AbuAdelERP.Api.Services
{
    public interface IFinishedGoodsTransferService
    {
        Task<List<FinishedGoodsTransferViewDto>> GetAllTransfersAsync();
        Task<TransferLookupsDto> GetLookupsAsync();
        Task<bool> CreateTransferAsync(CreateTransferRequestDto dto);
        Task<bool> AuditCostingAsync(CostingAuditDto dto);
        Task<ResolveCostingDisputeDto> ResolveCostingDisputeAsync(ResolveCostingDisputeDto dto);
        Task<bool> InspectQcAsync(QcInspectionDto dto);
        Task<bool> ReSubmitReworkAsync(long transferId, decimal adjustedQty, int adjustedCount, string notes);
        Task<bool> ReceiveAtControlAsync(ControlReceiptDto dto);
        Task<bool> ResolveDisputeAsync(ResolveDisputeDto dto);
    }

    public class FinishedGoodsTransferService : IFinishedGoodsTransferService
    {
        private readonly AppDbContext _context;

        public FinishedGoodsTransferService(AppDbContext context)
        {
            _context = context;
        }

        // ========================================================
        // 1. استعراض كافة أذونات تسليم المنتج التام
        // ========================================================
        public async Task<List<FinishedGoodsTransferViewDto>> GetAllTransfersAsync()
        {
            return await _context.FinishedGoodsTransfers
                .AsNoTracking()
                .Include(t => t.SourceWarehouse)
                .Include(t => t.DestinationWarehouse)
                .Include(t => t.Item)
                .OrderByDescending(t => t.CreatedAt)
                .Select(t => new FinishedGoodsTransferViewDto
                {
                    Id = t.Id,
                    TransferNumber = t.TransferNumber,
                    TransferDate = t.TransferDate,
                    SourceWarehouseName = t.SourceWarehouse != null ? t.SourceWarehouse.WarehouseName : "صالة التشغيل",
                    DestinationWarehouseName = t.DestinationWarehouse != null ? t.DestinationWarehouse.WarehouseName : "مخزن الكنترول",
                    ItemName = t.Item != null ? t.Item.ItemName : "صنف",
                    ItemCode = t.Item != null ? t.Item.ItemCode : string.Empty,
                    BatchNumber = t.BatchNumber,
                    ExpiryDate = t.ExpiryDate,
                    ProdDispatchedQty = t.ProdDispatchedQty,
                    ProdDispatchedCount = t.ProdDispatchedCount,
                    ProdNotes = t.ProdNotes,
                    CostingActualQty = t.CostingActualQty,
                    CostingActualCount = t.CostingActualCount,
                    CostingWeightDiff = t.CostingWeightDiff,
                    CostingCountDiff = t.CostingCountDiff,
                    CostingDiscrepancyFlag = t.CostingDiscrepancyFlag,
                    CostingDisputeStatus = t.CostingDisputeStatus,
                    CostingDisputeReason = t.CostingDisputeReason,
                    CostingDisputeChargeTo = t.CostingDisputeChargeTo,
                    QcApprovedQty = t.QcApprovedQty,
                    QcSampleQty = t.QcSampleQty,
                    QcReworkQty = t.QcReworkQty,
                    QcScrapQty = t.QcScrapQty,
                    ReceivedActualQty = t.ReceivedActualQty,
                    ReceivedActualCount = t.ReceivedActualCount,
                    HasDispute = t.HasDispute,
                    DisputeReason = t.DisputeReason,
                    DisputeStatus = t.DisputeStatus,
                    ControlDisputeChargeTo = t.ControlDisputeChargeTo,
                    Status = t.Status
                })
                .ToListAsync();
        }

        // ========================================================
        // 2. القوائم المرجعية لشاشات تسليم المنتج التام
        // ========================================================
        public async Task<TransferLookupsDto> GetLookupsAsync()
        {
            var warehouses = await _context.Warehouses
                .AsNoTracking()
                .Where(w => w.Active && !w.WarehouseName.Contains("مرتجع") && !w.WarehouseName.Contains("تالف"))
                .OrderBy(w => w.WarehouseName)
                .Select(w => new WarehouseLookupDto
                {
                    Id = w.Id,
                    WarehouseName = w.WarehouseName,
                    WarehouseCode = w.WarehouseCode,
                    WarehouseType = w.WarehouseType
                })
                .ToListAsync();

            var finishedItems = await _context.Items
                .AsNoTracking()
                .Where(i => i.Active && (i.ItemType == "منتج تام" || i.ItemType == "FINISHED_GOODS" || i.ItemType == "وسيط" || i.ItemType == "نصف مصنع"))
                .OrderBy(i => i.ItemName)
                .Select(i => new FinishedItemLookupDto
                {
                    Id = i.Id,
                    ItemCode = i.ItemCode,
                    ItemName = i.ItemName,
                    ItemType = i.ItemType,
                    Unit = i.Unit,
                    DepartmentId = i.DepartmentId,
                    ShelfLifeDays = i.ShelfLifeDays
                })
                .ToListAsync();

            return new TransferLookupsDto
            {
                Warehouses = warehouses,
                FinishedItems = finishedItems
            };
        }

        // ========================================================
        // 3. المحطة 1: إصدار إذن تسليم من صالة التشغيل
        // ========================================================
        public async Task<bool> CreateTransferAsync(CreateTransferRequestDto dto)
        {
            if (dto.SourceWarehouseId == dto.DestinationWarehouseId)
                throw new InvalidOperationException("لا يمكن التحويل لنفس المستودع.");

            if (dto.DispatchedQty <= 0 && dto.DispatchedCount <= 0)
                throw new InvalidOperationException("يجب إدخال كمية وزنية أو عدد صاجات أكبر من الصفر.");

            var transfer = new FinishedGoodsTransfer
            {
                TransferNumber = $"TRF-{DateTime.UtcNow:yyyyMMdd}-{new Random().Next(100, 999)}",
                TransferDate = DateOnly.FromDateTime(DateTime.UtcNow),
                SourceWarehouseId = dto.SourceWarehouseId,
                DestinationWarehouseId = dto.DestinationWarehouseId,
                ItemId = dto.ItemId,
                BatchNumber = string.IsNullOrWhiteSpace(dto.BatchNumber) ? $"LOT-{DateTime.UtcNow:yyyyMMdd}-{dto.ItemId}" : dto.BatchNumber.Trim(),
                ExpiryDate = dto.ExpiryDate,
                ProdDispatchedQty = dto.DispatchedQty,
                ProdDispatchedCount = dto.DispatchedCount,
                ProdNotes = dto.Notes,
                ProdSubmittedBy = dto.SubmittedBy,
                Status = "PENDING_COSTING",
                CreatedAt = DateTime.UtcNow
            };

            _context.FinishedGoodsTransfers.Add(transfer);
            await _context.SaveChangesAsync();
            return true;
        }

        // ========================================================
        // 4. المحطة 2: تدقيق ميزان التكاليف
        // ========================================================
        public async Task<bool> AuditCostingAsync(CostingAuditDto dto)
        {
            var trf = await _context.FinishedGoodsTransfers.FindAsync(dto.TransferId);
            if (trf == null) return false;

            decimal weightDiff = dto.ActualWeight - trf.ProdDispatchedQty;
            int countDiff = dto.ActualCount - trf.ProdDispatchedCount;

            bool hasWeightDiscrepancy = Math.Abs(weightDiff) > (trf.ProdDispatchedQty * 0.005m);
            bool hasCountDiscrepancy = countDiff != 0;

            trf.CostingActualQty = dto.ActualWeight;
            trf.CostingActualCount = dto.ActualCount;
            trf.CostingWeightDiff = weightDiff;
            trf.CostingCountDiff = countDiff;
            trf.CostingDiscrepancyFlag = hasWeightDiscrepancy || hasCountDiscrepancy;
            trf.CostingAuditor = dto.AuditorName;
            trf.CostingAuditedAt = DateTime.UtcNow;
            trf.CostingNotes = dto.Notes;

            if (dto.EscalateDispute && (hasWeightDiscrepancy || hasCountDiscrepancy))
            {
                trf.Status = "DISPUTE_COSTING";
                trf.CostingDisputeStatus = "OPEN_DISPUTE";
                trf.CostingDisputeReason = $"تضارب أعداد أو أوزان: تسليم الصالة ({trf.ProdDispatchedQty:N2} | {trf.ProdDispatchedCount} صاج) مقابل ميزان التكاليف ({dto.ActualWeight:N2} | {dto.ActualCount} صاج) - فرق وزن: {weightDiff:N2}، فرق عدد: {countDiff}.";
            }
            else
            {
                trf.Status = "PENDING_QC";
            }

            await _context.SaveChangesAsync();
            return true;
        }

        // ========================================================
        // 5. فض نزاع ميزان التكاليف والإنتاج
        // ========================================================
        public async Task<ResolveCostingDisputeDto> ResolveCostingDisputeAsync(ResolveCostingDisputeDto dto)
        {
            var trf = await _context.FinishedGoodsTransfers.FindAsync(dto.TransferId);
            if (trf == null) return dto;

            trf.CostingActualQty = dto.FinalAcceptedWeight;
            trf.CostingActualCount = dto.FinalAcceptedCount;
            trf.CostingDisputeStatus = "RESOLVED";
            trf.CostingDisputeResolvedBy = dto.ResolvedBy;
            trf.CostingDisputeResolvedAt = DateTime.UtcNow;
            trf.CostingDisputeChargeTo = dto.ChargeTo;
            trf.CostingDisputeNotes = dto.ResolutionNotes;
            trf.Status = "PENDING_QC";

            await _context.SaveChangesAsync();
            return dto;
        }

        // ========================================================
        // 6. المحطة 3: فحص وإفراج الجودة (GMP / QC)
        // ========================================================
        public async Task<bool> InspectQcAsync(QcInspectionDto dto)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var trf = await _context.FinishedGoodsTransfers.FindAsync(dto.TransferId);
                if (trf == null) return false;

                decimal baseCosting = trf.CostingActualQty ?? trf.ProdDispatchedQty;
                decimal totalDeductions = dto.SampleQty + dto.ReworkQty + dto.ScrapQty;

                if (totalDeductions > baseCosting)
                {
                    throw new InvalidOperationException($"مجموع العينات والمرتجع والهالك ({totalDeductions:N2}) يتجاوز الوزن المعتمد من التكاليف ({baseCosting:N2})!");
                }

                decimal netApproved = baseCosting - totalDeductions;

                trf.QcApprovedQty = netApproved;
                trf.QcSampleQty = dto.SampleQty;
                trf.QcReworkQty = dto.ReworkQty;
                trf.QcScrapQty = dto.ScrapQty;
                trf.QcInspector = dto.InspectorName;
                trf.QcInspectedAt = DateTime.UtcNow;
                trf.QcNotes = dto.Notes;
                trf.Status = "PENDING_RECEIPT";

                if (dto.ReworkQty > 0)
                {
                    var reworkTransfer = new FinishedGoodsTransfer
                    {
                        TransferNumber = $"{trf.TransferNumber}-RW",
                        TransferDate = DateOnly.FromDateTime(DateTime.UtcNow),
                        SourceWarehouseId = trf.SourceWarehouseId,
                        DestinationWarehouseId = trf.DestinationWarehouseId,
                        ItemId = trf.ItemId,
                        BatchNumber = $"{trf.BatchNumber}-RW",
                        ExpiryDate = trf.ExpiryDate,
                        ProdDispatchedQty = dto.ReworkQty,
                        ProdDispatchedCount = 0,
                        ProdNotes = $"مرتجع للتعديل من الإذن {trf.TransferNumber} | ملاحظات الفحص: {dto.Notes}",
                        ProdSubmittedBy = "إدارة توكيد الجودة",
                        Status = "REWORK_IN_PRODUCTION",
                        CreatedAt = DateTime.UtcNow
                    };
                    _context.FinishedGoodsTransfers.Add(reworkTransfer);
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

        // ========================================================
        // 7. المحطة 4: إعادة إرسال المرتجع بعد التعديل بالصالة
        // ========================================================
        public async Task<bool> ReSubmitReworkAsync(long transferId, decimal adjustedQty, int adjustedCount, string notes)
        {
            var trf = await _context.FinishedGoodsTransfers.FindAsync(transferId);
            if (trf == null) return false;

            trf.ProdDispatchedQty = adjustedQty;
            trf.ProdDispatchedCount = adjustedCount;
            trf.ProdNotes = $"{trf.ProdNotes} | محضر التعديل: {notes}";
            trf.Status = "PENDING_COSTING";

            await _context.SaveChangesAsync();
            return true;
        }

        // ========================================================
        // 8. المحطة 5: استلام وترصيد مخزن الكنترول
        // ========================================================
        public async Task<bool> ReceiveAtControlAsync(ControlReceiptDto dto)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var trf = await _context.FinishedGoodsTransfers.FindAsync(dto.TransferId);
                if (trf == null) throw new InvalidOperationException("إذن التحويل غير موجود.");

                trf.ReceivedActualQty = dto.ReceivedQty;
                trf.ReceivedActualCount = dto.ReceivedCount;
                trf.ControlReceiverName = dto.ReceiverName;
                trf.ControlReceivedAt = DateTime.UtcNow;
                trf.ControlNotes = dto.Notes;

                if (dto.ReceivedQty < trf.QcApprovedQty)
                {
                    decimal deficit = trf.QcApprovedQty - dto.ReceivedQty;
                    trf.HasDispute = true;
                    trf.DisputeReason = $"عجز غير مبرر: المفرج عنه من الجودة {trf.QcApprovedQty:N2}، بينما المستلم فعلياً {dto.ReceivedQty:N2} (فرق {deficit:N2}).";
                    trf.DisputeStatus = "OPEN_DISPUTE";
                    trf.DisputeOpenedAt = DateTime.UtcNow;
                    trf.Status = "IN_DISPUTE";

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();
                    return true;
                }

                await FinalizeInventoryMovements(trf, dto.ReceivedQty);
                trf.Status = "COMPLETED";

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

        // ========================================================
        // 9. فض نزاع الجودة والكنترول واعتماد التسوية
        // ========================================================
        public async Task<bool> ResolveDisputeAsync(ResolveDisputeDto dto)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var trf = await _context.FinishedGoodsTransfers.FindAsync(dto.TransferId);
                if (trf == null) return false;

                trf.ReceivedActualQty = dto.FinalAcceptedQty;
                trf.DisputeStatus = "RESOLVED";
                trf.DisputeResolvedBy = dto.ResolvedBy;
                trf.DisputeResolutionNotes = dto.ResolutionNotes;
                trf.DisputeResolvedAt = DateTime.UtcNow;
                trf.ControlDisputeChargeTo = dto.ChargeTo;

                await FinalizeInventoryMovements(trf, dto.FinalAcceptedQty);
                trf.Status = "COMPLETED";

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

        // ========================================================
        // 10. الترحيل المخزني الذري والآمن لكافة الحركات والأرصدة
        // ========================================================
        private async Task FinalizeInventoryMovements(FinishedGoodsTransfer trf, decimal acceptedControlQty)
        {
            decimal baseWeight = trf.CostingActualQty ?? trf.ProdDispatchedQty;
            decimal totalLeftFloor = baseWeight - trf.QcReworkQty;
            DateTime now = DateTime.UtcNow;

            var item = await _context.Items.AsNoTracking().FirstOrDefaultAsync(i => i.Id == trf.ItemId);
            decimal itemUnitCost = item?.BasePrice ?? 0m;
            long companyId = (item != null && item.CompanyId > 0) ? item.CompanyId : 1;

            // 1. خصم الرصيد من صالة التشغيل (Source Warehouse)
            var sourceBal = await _context.StockBalances
                .FirstOrDefaultAsync(b => b.ItemId == trf.ItemId && b.WarehouseId == trf.SourceWarehouseId);

            if (sourceBal != null)
            {
                sourceBal.Quantity -= totalLeftFloor;
                sourceBal.AvailableQuantity = sourceBal.Quantity - sourceBal.ReservedQuantity;
                sourceBal.LastUpdatedAt = now;
            }
            else
            {
                _context.StockBalances.Add(new StockBalance
                {
                    CompanyId = companyId,
                    WarehouseId = trf.SourceWarehouseId,
                    ItemId = trf.ItemId,
                    Quantity = -totalLeftFloor,
                    AvailableQuantity = -totalLeftFloor,
                    ReservedQuantity = 0,
                    LastUpdatedAt = now
                });
            }

            // حركة خروج من الصالة
            _context.StockMovements.Add(new StockMovement
            {
                CompanyId = companyId,
                WarehouseId = trf.SourceWarehouseId,
                ItemId = trf.ItemId,
                MovementType = "FG_TRANSFER_OUT",
                Quantity = acceptedControlQty,
                UnitCost = itemUnitCost,
                MovementAt = now,
                ReferenceType = "FG_TRANSFER",
                ReferenceId = trf.Id,
                Notes = $"تحويل تام إلى مخزن الكنترول | إذن: {trf.TransferNumber} | تشغيلة: {trf.BatchNumber}",
                CreatedAt = now
            });

            // 2. إضافة الرصيد إلى مخزن الكنترول (Destination Warehouse)
            var destBal = await _context.StockBalances
                .FirstOrDefaultAsync(b => b.ItemId == trf.ItemId && b.WarehouseId == trf.DestinationWarehouseId);

            if (destBal != null)
            {
                destBal.Quantity += acceptedControlQty;
                destBal.AvailableQuantity = destBal.Quantity - destBal.ReservedQuantity;
                destBal.LastUpdatedAt = now;
            }
            else
            {
                _context.StockBalances.Add(new StockBalance
                {
                    CompanyId = companyId,
                    WarehouseId = trf.DestinationWarehouseId,
                    ItemId = trf.ItemId,
                    Quantity = acceptedControlQty,
                    AvailableQuantity = acceptedControlQty,
                    ReservedQuantity = 0,
                    LastUpdatedAt = now
                });
            }

            string expFormatted = trf.ExpiryDate.HasValue ? trf.ExpiryDate.Value.ToString("yyyy-MM-dd") : "غير محدد";

            // حركة دخول الكنترول
            _context.StockMovements.Add(new StockMovement
            {
                CompanyId = companyId,
                WarehouseId = trf.DestinationWarehouseId,
                ItemId = trf.ItemId,
                MovementType = "FG_TRANSFER_IN",
                Quantity = acceptedControlQty,
                UnitCost = itemUnitCost,
                MovementAt = now,
                ReferenceType = "FG_TRANSFER",
                ReferenceId = trf.Id,
                Notes = $"استلام منتج تام بالكنترول | إذن: {trf.TransferNumber} | تشغيلة: {trf.BatchNumber} | صلاحية: {expFormatted}",
                CreatedAt = now
            });

            // 3. إثبات عينات فحص الجودة
            if (trf.QcSampleQty > 0)
            {
                _context.StockMovements.Add(new StockMovement
                {
                    CompanyId = companyId,
                    WarehouseId = trf.SourceWarehouseId,
                    ItemId = trf.ItemId,
                    MovementType = "QC_SAMPLE",
                    Quantity = trf.QcSampleQty,
                    UnitCost = itemUnitCost,
                    MovementAt = now,
                    ReferenceType = "FG_TRANSFER",
                    ReferenceId = trf.Id,
                    Notes = $"سحب عينات فحص معملي | إذن: {trf.TransferNumber}",
                    CreatedAt = now
                });
            }

            // 4. إثبات هالك الجودة غير المطابق
            if (trf.QcScrapQty > 0)
            {
                _context.StockMovements.Add(new StockMovement
                {
                    CompanyId = companyId,
                    WarehouseId = trf.SourceWarehouseId,
                    ItemId = trf.ItemId,
                    MovementType = "QC_SCRAP",
                    Quantity = trf.QcScrapQty,
                    UnitCost = itemUnitCost,
                    MovementAt = now,
                    ReferenceType = "FG_TRANSFER",
                    ReferenceId = trf.Id,
                    Notes = $"إهلاك فني غير مطابق للجودة | إذن: {trf.TransferNumber}",
                    CreatedAt = now
                });
            }

            // 5. إثبات عجز النقل والعبور
            decimal transitDeficit = trf.QcApprovedQty - acceptedControlQty;
            if (transitDeficit > 0)
            {
                _context.StockMovements.Add(new StockMovement
                {
                    CompanyId = companyId,
                    WarehouseId = trf.SourceWarehouseId,
                    ItemId = trf.ItemId,
                    MovementType = "FG_TRANSIT_DEFICIT",
                    Quantity = transitDeficit,
                    UnitCost = itemUnitCost,
                    MovementAt = now,
                    ReferenceType = "FG_TRANSFER",
                    ReferenceId = trf.Id,
                    Notes = $"تسوية عجز استلام كنترول | إذن: {trf.TransferNumber} | محمّل على: {trf.ControlDisputeChargeTo ?? "لجنة فض المنازعات"}",
                    CreatedAt = now
                });
            }
        }
    }
}