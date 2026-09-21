using Microsoft.EntityFrameworkCore;
using AbuAdelERP.Api.Models;
using AbuAdelERP.Api.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AbuAdelERP.Api.Services
{
    public interface IFinishedGoodsTransferService
    {
        Task<List<FinishedGoodsTransferViewDto>> GetAllTransfersAsync();
        Task<bool> CreateTransferAsync(CreateTransferRequestDto dto);
        Task<bool> AuditCostingAsync(CostingAuditDto dto);
        Task<bool> ResolveCostingDisputeAsync(ResolveCostingDisputeDto dto);
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

        public async Task<List<FinishedGoodsTransferViewDto>> GetAllTransfersAsync()
        {
            return await _context.FinishedGoodsTransfers
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
                    ItemCode = t.Item != null ? t.Item.ItemCode : "",
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

        public async Task<bool> CreateTransferAsync(CreateTransferRequestDto dto)
        {
            var transfer = new FinishedGoodsTransfer
            {
                TransferNumber = $"TRF-{DateTime.UtcNow:yyyyMMdd}-{new Random().Next(100, 999)}",
                TransferDate = DateOnly.FromDateTime(DateTime.Today),
                SourceWarehouseId = dto.SourceWarehouseId,
                DestinationWarehouseId = dto.DestinationWarehouseId,
                ItemId = dto.ItemId,
                BatchNumber = dto.BatchNumber,
                ExpiryDate = dto.ExpiryDate,
                ProdDispatchedQty = dto.DispatchedQty,
                ProdDispatchedCount = dto.DispatchedCount,
                ProdNotes = dto.Notes,
                ProdSubmittedBy = dto.SubmittedBy,
                Status = "PENDING_COSTING"
            };

            _context.FinishedGoodsTransfers.Add(transfer);
            await _context.SaveChangesAsync();
            return true;
        }

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

        public async Task<bool> ResolveCostingDisputeAsync(ResolveCostingDisputeDto dto)
        {
            var trf = await _context.FinishedGoodsTransfers.FindAsync(dto.TransferId);
            if (trf == null) return false;

            trf.CostingActualQty = dto.FinalAcceptedWeight;
            trf.CostingActualCount = dto.FinalAcceptedCount;
            trf.CostingDisputeStatus = "RESOLVED";
            trf.CostingDisputeResolvedBy = dto.ResolvedBy;
            trf.CostingDisputeResolvedAt = DateTime.UtcNow;
            trf.CostingDisputeChargeTo = dto.ChargeTo;
            trf.CostingDisputeNotes = dto.ResolutionNotes;
            trf.Status = "PENDING_QC";

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> InspectQcAsync(QcInspectionDto dto)
        {
            var trf = await _context.FinishedGoodsTransfers.FindAsync(dto.TransferId);
            if (trf == null) return false;

            decimal baseCosting = trf.CostingActualQty ?? trf.ProdDispatchedQty;
            decimal totalDeductions = dto.SampleQty + dto.ReworkQty + dto.ScrapQty;

            if (totalDeductions > baseCosting)
            {
                throw new Exception($"مجموع العينات والمرتجع والهالك ({totalDeductions:N2}) يتجاوز الوزن المعتمد من التكاليف ({baseCosting:N2})!");
            }

            // معادلة الكتلة الصارمة: المقبول الصافي يُحسب آلياً
            decimal netApproved = baseCosting - totalDeductions;

            trf.QcApprovedQty = netApproved;
            trf.QcSampleQty = dto.SampleQty;
            trf.QcReworkQty = dto.ReworkQty;
            trf.QcScrapQty = dto.ScrapQty;
            trf.QcInspector = dto.InspectorName;
            trf.QcInspectedAt = DateTime.UtcNow;
            trf.QcNotes = dto.Notes;
            trf.Status = "PENDING_RECEIPT";

            // توليد إذن إعادة تشغيل وتعديل للصالة آلياً في حال وجود مرتجع
            if (dto.ReworkQty > 0)
            {
                var reworkTransfer = new FinishedGoodsTransfer
                {
                    TransferNumber = $"{trf.TransferNumber}-RW",
                    TransferDate = DateOnly.FromDateTime(DateTime.Today),
                    SourceWarehouseId = trf.SourceWarehouseId,
                    DestinationWarehouseId = trf.DestinationWarehouseId,
                    ItemId = trf.ItemId,
                    BatchNumber = $"{trf.BatchNumber}-RW",
                    ExpiryDate = trf.ExpiryDate,
                    ProdDispatchedQty = dto.ReworkQty,
                    ProdDispatchedCount = 0,
                    ProdNotes = $"مرتجع للتعديل من الإذن {trf.TransferNumber} | ملاحظات الفحص: {dto.Notes}",
                    ProdSubmittedBy = "إدارة توكيد الجودة",
                    Status = "REWORK_IN_PRODUCTION"
                };
                _context.FinishedGoodsTransfers.Add(reworkTransfer);
            }

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ReSubmitReworkAsync(long transferId, decimal adjustedQty, int adjustedCount, string notes)
        {
            var trf = await _context.FinishedGoodsTransfers.FindAsync(transferId);
            if (trf == null) return false;

            trf.ProdDispatchedQty = adjustedQty;
            trf.ProdDispatchedCount = adjustedCount;
            trf.ProdNotes = $"{trf.ProdNotes} | محضر التعديل: {notes}";
            trf.Status = "PENDING_COSTING"; // عودة الدفعة للميزان

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ReceiveAtControlAsync(ControlReceiptDto dto)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var trf = await _context.FinishedGoodsTransfers.FindAsync(dto.TransferId);
                if (trf == null) throw new Exception("إذن التحويل غير موجود.");

                trf.ReceivedActualQty = dto.ReceivedQty;
                trf.ReceivedActualCount = dto.ReceivedCount;
                trf.ControlReceiverName = dto.ReceiverName;
                trf.ControlReceivedAt = DateTime.UtcNow;
                trf.ControlNotes = dto.Notes;

                // فحص العجز غير المبرر بالجودة
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

                // تنفيذ الحركات وترصيد الكنترول
                await FinalizeInventoryMovements(trf, dto.ReceivedQty);
                trf.Status = "COMPLETED";

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                throw new Exception($"فشل اعتماد استلام الكنترول: {ex.Message}");
            }
        }

        public async Task<bool> ResolveDisputeAsync(ResolveDisputeDto dto)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var trf = await _context.FinishedGoodsTransfers.FindAsync(dto.TransferId);
                if (trf == null) return false;

                trf.ReceivedActualQty = dto.FinalAcceptedQty; // تحديث الكمية الفعلية النهائية
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

        private async Task FinalizeInventoryMovements(FinishedGoodsTransfer trf, decimal acceptedControlQty)
        {
            // إجمالي ما غادر صالة الإنتاج = المعتمد للتحويل ناقصاً ما احتُجز بالصالة للتعديل
            decimal baseWeight = trf.CostingActualQty ?? trf.ProdDispatchedQty;
            decimal totalLeftFloor = baseWeight - trf.QcReworkQty;

            // 1. تحديث أو إنشاء رصيد مخزن الصالة (StockBalance)
            var sourceBal = await _context.StockBalances
                .FirstOrDefaultAsync(b => b.ItemId == trf.ItemId && b.WarehouseId == trf.SourceWarehouseId);

            if (sourceBal != null)
            {
                sourceBal.Quantity -= totalLeftFloor;
                sourceBal.AvailableQuantity -= totalLeftFloor;
                sourceBal.LastUpdatedAt = DateTime.UtcNow;
            }
            else
            {
                // ضمان إنشاء سطر الصالة حتى لو لم يكن له رصيد افتتاحي
                _context.StockBalances.Add(new StockBalance
                {
                    CompanyId = 1,
                    WarehouseId = trf.SourceWarehouseId,
                    ItemId = trf.ItemId,
                    Quantity = -totalLeftFloor,
                    AvailableQuantity = -totalLeftFloor,
                    ReservedQuantity = 0,
                    LastUpdatedAt = DateTime.UtcNow
                });
            }

            // حركة خروج المنتج التام من الصالة
            _context.StockMovements.Add(new StockMovement
            {
                CompanyId = 1,
                WarehouseId = trf.SourceWarehouseId,
                ItemId = trf.ItemId,
                MovementType = "FG_TRANSFER_OUT",
                Quantity = acceptedControlQty,
                MovementAt = DateTime.UtcNow,
                ReferenceType = "FG_TRANSFER",
                ReferenceId = trf.Id,
                Notes = $"تحويل تام إلى مخزن الكنترول | إذن: {trf.TransferNumber} | تشغيلة: {trf.BatchNumber}"
            });

            // 2. تحديث رصيد مخزن الكنترول الرئيسي
            var destBal = await _context.StockBalances
                .FirstOrDefaultAsync(b => b.ItemId == trf.ItemId && b.WarehouseId == trf.DestinationWarehouseId);

            if (destBal != null)
            {
                destBal.Quantity += acceptedControlQty;
                destBal.AvailableQuantity += acceptedControlQty;
                destBal.LastUpdatedAt = DateTime.UtcNow;
            }
            else
            {
                _context.StockBalances.Add(new StockBalance
                {
                    CompanyId = 1,
                    WarehouseId = trf.DestinationWarehouseId,
                    ItemId = trf.ItemId,
                    Quantity = acceptedControlQty,
                    AvailableQuantity = acceptedControlQty,
                    ReservedQuantity = 0,
                    LastUpdatedAt = DateTime.UtcNow
                });
            }

            string expFormatted = trf.ExpiryDate.HasValue ? trf.ExpiryDate.Value.ToString("yyyy-MM-dd") : "غير محدد";
            _context.StockMovements.Add(new StockMovement
            {
                CompanyId = 1,
                WarehouseId = trf.DestinationWarehouseId,
                ItemId = trf.ItemId,
                MovementType = "FG_TRANSFER_IN",
                Quantity = acceptedControlQty,
                MovementAt = DateTime.UtcNow,
                ReferenceType = "FG_TRANSFER",
                ReferenceId = trf.Id,
                Notes = $"استلام منتج تام بالكنترول | إذن: {trf.TransferNumber} | تشغيلة: {trf.BatchNumber} | صلاحية: {expFormatted}"
            });

            // 3. قيد عينات الجودة المعملية
            if (trf.QcSampleQty > 0)
            {
                _context.StockMovements.Add(new StockMovement
                {
                    CompanyId = 1,
                    WarehouseId = trf.SourceWarehouseId,
                    ItemId = trf.ItemId,
                    MovementType = "QC_SAMPLE",
                    Quantity = trf.QcSampleQty,
                    MovementAt = DateTime.UtcNow,
                    ReferenceType = "FG_TRANSFER",
                    ReferenceId = trf.Id,
                    Notes = $"سحب عينات فحص معملي | إذن: {trf.TransferNumber}"
                });
            }

            // 4. قيد الهالك الفني المعتمد
            if (trf.QcScrapQty > 0)
            {
                _context.StockMovements.Add(new StockMovement
                {
                    CompanyId = 1,
                    WarehouseId = trf.SourceWarehouseId,
                    ItemId = trf.ItemId,
                    MovementType = "QC_SCRAP",
                    Quantity = trf.QcScrapQty,
                    MovementAt = DateTime.UtcNow,
                    ReferenceType = "FG_TRANSFER",
                    ReferenceId = trf.Id,
                    Notes = $"إهلاك فني غير مطابق للجودة | إذن: {trf.TransferNumber}"
                });
            }

            // 5. قيد عجز النزاع المحمّل باللجنة (إن وجد عجز بين الجودة والكنترول)
            decimal transitDeficit = trf.QcApprovedQty - acceptedControlQty;
            if (transitDeficit > 0)
            {
                _context.StockMovements.Add(new StockMovement
                {
                    CompanyId = 1,
                    WarehouseId = trf.SourceWarehouseId,
                    ItemId = trf.ItemId,
                    MovementType = "FG_TRANSIT_DEFICIT",
                    Quantity = transitDeficit,
                    MovementAt = DateTime.UtcNow,
                    ReferenceType = "FG_TRANSFER",
                    ReferenceId = trf.Id,
                    Notes = $"تسوية عجز استلام كنترول | إذن: {trf.TransferNumber} | محمّل على: {trf.ControlDisputeChargeTo ?? "لجنة فض المنازعات"}"
                });
            }
        }
    }
}