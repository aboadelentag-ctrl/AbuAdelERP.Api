using Microsoft.EntityFrameworkCore;
using AbuAdelERP.Api.Models;
using AbuAdelERP.Api.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AbuAdelERP.Api.Services
{
    public interface IMaterialRequisitionService
    {
        Task<List<RequisitionDetailDto>> GetAllRequisitionsAsync(string? status = null);
        Task<RequisitionDetailDto?> GetRequisitionByIdAsync(long id);
        Task<RequisitionDetailDto> CreateRequisitionAsync(CreateRequisitionDto dto);
        Task<List<FefoBatchSuggestionDto>> GetFefoSuggestionsAsync(long itemId, long warehouseId);
        Task<bool> DispatchRequisitionAsync(DispatchRequisitionDto dto);
        Task<bool> ReceiveLineItemAsync(ReceiveLineItemDto dto);
        Task<bool> ResolveDiscrepancyAsync(ResolveDiscrepancyDto dto);
    }

    public class MaterialRequisitionService : IMaterialRequisitionService
    {
        private readonly AppDbContext _context;

        public MaterialRequisitionService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<RequisitionDetailDto>> GetAllRequisitionsAsync(string? status = null)
        {
            var query = _context.MaterialRequisitions
                .Include(r => r.Department)
                .Include(r => r.FromWarehouse)
                .Include(r => r.Lines).ThenInclude(l => l.Item)
                .AsNoTracking();

            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(r => r.Status == status);
            }

            var list = await query.OrderByDescending(r => r.CreatedAt).ToListAsync();
            return list.Select(MapToDetailDto).ToList();
        }

        public async Task<RequisitionDetailDto?> GetRequisitionByIdAsync(long id)
        {
            var req = await _context.MaterialRequisitions
                .Include(r => r.Department)
                .Include(r => r.FromWarehouse)
                .Include(r => r.Lines).ThenInclude(l => l.Item)
                .FirstOrDefaultAsync(r => r.Id == id);

            return req == null ? null : MapToDetailDto(req);
        }

        public async Task<RequisitionDetailDto> CreateRequisitionAsync(CreateRequisitionDto dto)
        {
            string reqNumber = $"MR-{DateTime.UtcNow:yyyyMMdd}-{new Random().Next(1000, 9999)}";

            var req = new MaterialRequisition
            {
                RequisitionNumber = reqNumber,
                DepartmentId = dto.DepartmentId,
                FromWarehouseId = dto.FromWarehouseId,
                Status = "SUBMITTED",
                Notes = dto.Notes,
                RequestedBy = dto.RequestedBy,
                CreatedAt = DateTime.UtcNow
            };

            foreach (var line in dto.Lines)
            {
                req.Lines.Add(new MaterialRequisitionLine
                {
                    ItemId = line.ItemId,
                    RequestedQty = line.RequestedQty,
                    LineStatus = "PENDING",
                    CreatedAt = DateTime.UtcNow
                });
            }

            _context.MaterialRequisitions.Add(req);
            await _context.SaveChangesAsync();

            return (await GetRequisitionByIdAsync(req.Id))!;
        }

        public async Task<List<FefoBatchSuggestionDto>> GetFefoSuggestionsAsync(long itemId, long warehouseId)
        {
            var receipts = await _context.StockMovements
                .Where(m => m.ItemId == itemId && m.WarehouseId == warehouseId && m.MovementType == "PURCHASE_RECEIPT")
                .OrderBy(m => m.MovementAt)
                .ToListAsync();

            var suggestions = new List<FefoBatchSuggestionDto>();

            foreach (var r in receipts)
            {
                if (string.IsNullOrWhiteSpace(r.Notes)) continue;

                string batch = ExtractValue(r.Notes, "تشغيلة:", "|");
                string expStr = ExtractValue(r.Notes, "صلاحية:", "|");

                DateTime? expDate = null;
                if (DateTime.TryParse(expStr, out DateTime parsed)) expDate = parsed;

                suggestions.Add(new FefoBatchSuggestionDto
                {
                    BatchNumber = !string.IsNullOrWhiteSpace(batch) ? batch : "لوط افتراضي",
                    ExpiryDate = expDate,
                    AvailableQty = Convert.ToDecimal(r.Quantity),
                    UnitCost = Convert.ToDecimal(r.UnitCost)
                });
            }

            return suggestions.OrderBy(s => s.ExpiryDate ?? DateTime.MaxValue).ToList();
        }

        public async Task<bool> DispatchRequisitionAsync(DispatchRequisitionDto dto)
        {
            var req = await _context.MaterialRequisitions
                .Include(r => r.Lines)
                .FirstOrDefaultAsync(r => r.Id == dto.RequisitionId);

            if (req == null) return false;

            req.Status = "DISPATCHED";
            req.DispatchedBy = dto.DispatchedBy;
            req.DispatchedAt = DateTime.UtcNow;

            foreach (var dl in dto.Lines)
            {
                var line = req.Lines.FirstOrDefault(l => l.Id == dl.LineId);
                if (line != null)
                {
                    line.DispatchedQty = dl.DispatchedQty;
                    line.BatchNumber = dl.BatchNumber;
                    line.ExpiryDate = dl.ExpiryDate.HasValue ? DateTime.SpecifyKind(dl.ExpiryDate.Value.Date, DateTimeKind.Utc) : null;
                    line.UnitCost = dl.UnitCost;
                    line.LineStatus = "DISPATCHED";
                }
            }

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ReceiveLineItemAsync(ReceiveLineItemDto dto)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var line = await _context.MaterialRequisitionLines
                    .Include(l => l.Requisition)
                    .FirstOrDefaultAsync(l => l.Id == dto.LineId);

                if (line == null) return false;

                line.ReceivedQty = dto.ReceivedQty;

                if (dto.IsAccepted && dto.ReceivedQty == line.DispatchedQty)
                {
                    line.LineStatus = "ACCEPTED";
                    line.DiscrepancyReason = null;
                }
                else
                {
                    line.LineStatus = "DISPUTED";
                    line.DiscrepancyReason = dto.DiscrepancyReason ?? "كمية غير مطابقة للمصروف";
                    line.Requisition!.Status = "DISPUTED";
                }

                line.Requisition!.ReceivedBy = dto.ReceivedBy;

                var allLines = await _context.MaterialRequisitionLines
                    .Where(l => l.RequisitionId == line.RequisitionId)
                    .ToListAsync();

                // إذا تطابقت كافة الخطوط، نغلق الطلب ونرحل المخزون ذرياً
                if (allLines.All(l => l.LineStatus == "ACCEPTED"))
                {
                    await FinalizeInventoryTransferAsync(line.Requisition!);
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

        public async Task<bool> ResolveDiscrepancyAsync(ResolveDiscrepancyDto dto)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var line = await _context.MaterialRequisitionLines
                    .Include(l => l.Requisition)
                    .FirstOrDefaultAsync(l => l.Id == dto.LineId);

                if (line == null) return false;

                line.ReceivedQty = dto.FinalApprovedQty;
                line.DispatchedQty = dto.FinalApprovedQty;
                line.WarehouseNotes = dto.WarehouseResolutionNotes;
                line.LineStatus = "ACCEPTED";

                var allLines = await _context.MaterialRequisitionLines
                    .Where(l => l.RequisitionId == line.RequisitionId)
                    .ToListAsync();

                if (allLines.All(l => l.LineStatus == "ACCEPTED"))
                {
                    var req = await _context.MaterialRequisitions.FindAsync(line.RequisitionId);
                    if (req != null)
                    {
                        await FinalizeInventoryTransferAsync(req);
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

        private async Task FinalizeInventoryTransferAsync(MaterialRequisition req)
        {
            req.Status = "COMPLETED";
            req.CompletedAt = DateTime.UtcNow;

            var lines = await _context.MaterialRequisitionLines
                .Include(l => l.Item)
                .Where(l => l.RequisitionId == req.Id)
                .ToListAsync();

            foreach (var line in lines)
            {
                // 1. خصم الكمية من رصيد المخزن الرئيسي
                var sourceBalance = await _context.StockBalances
                    .FirstOrDefaultAsync(b => b.ItemId == line.ItemId && b.WarehouseId == req.FromWarehouseId);

                if (sourceBalance != null)
                {
                    sourceBalance.Quantity -= line.ReceivedQty;
                    sourceBalance.AvailableQuantity -= line.ReceivedQty;
                }

                long itemCompanyId = (line.Item != null && line.Item.CompanyId > 0) ? line.Item.CompanyId : 1;

                // 2. تسجيل حركة الصرف في سجل الحركات بوجود معرّف الشركة لتفادي كسر الـ Foreign Key
                _context.StockMovements.Add(new StockMovement
                {
                    CompanyId = itemCompanyId,
                    ItemId = line.ItemId,
                    WarehouseId = req.FromWarehouseId,
                    MovementType = "PRODUCTION_ISSUE",
                    Quantity = line.ReceivedQty,
                    UnitCost = line.UnitCost,
                    ReferenceType = "MATERIAL_REQUISITION",
                    ReferenceId = req.Id,
                    MovementAt = DateTime.UtcNow,
                    Notes = $"صرف لصالة التشغيل | تشغيلة: {line.BatchNumber} | صلاحية: {line.ExpiryDate:yyyy-MM-dd} | إذن رقم: {req.RequisitionNumber}"
                });
            }
        }

        private static RequisitionDetailDto MapToDetailDto(MaterialRequisition r)
        {
            return new RequisitionDetailDto
            {
                Id = r.Id,
                RequisitionNumber = r.RequisitionNumber,
                DepartmentId = r.DepartmentId,
                DepartmentName = r.Department?.DepartmentName ?? "قسم غير محدد",
                FromWarehouseId = r.FromWarehouseId,
                FromWarehouseName = r.FromWarehouse?.WarehouseName ?? "المخزن الرئيسي",
                Status = r.Status,
                Notes = r.Notes,
                RequestedBy = r.RequestedBy,
                DispatchedBy = r.DispatchedBy,
                ReceivedBy = r.ReceivedBy,
                CreatedAt = r.CreatedAt,
                DispatchedAt = r.DispatchedAt,
                CompletedAt = r.CompletedAt,
                Lines = r.Lines.Select(l => new RequisitionLineDetailDto
                {
                    Id = l.Id,
                    ItemId = l.ItemId,
                    ItemCode = l.Item?.ItemCode ?? "",
                    ItemName = l.Item?.ItemName ?? "صنف",
                    Unit = l.Item?.Unit ?? "",
                    RequestedQty = l.RequestedQty,
                    DispatchedQty = l.DispatchedQty,
                    ReceivedQty = l.ReceivedQty,
                    BatchNumber = l.BatchNumber,
                    ExpiryDate = l.ExpiryDate,
                    UnitCost = l.UnitCost,
                    LineStatus = l.LineStatus,
                    DiscrepancyReason = l.DiscrepancyReason,
                    WarehouseNotes = l.WarehouseNotes
                }).ToList()
            };
        }

        private string ExtractValue(string text, string key, string separator)
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