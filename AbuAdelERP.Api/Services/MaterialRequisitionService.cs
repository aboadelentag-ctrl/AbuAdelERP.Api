using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using AbuAdelERP.Api.Models;
using AbuAdelERP.Api.DTOs;

namespace AbuAdelERP.Api.Services
{
    public interface IMaterialRequisitionService
    {
        Task<RequisitionLookupsDto> GetLookupsAsync();
        Task<List<RequisitionDetailDto>> GetAllRequisitionsAsync(string? status = null);
        Task<RequisitionDetailDto?> GetRequisitionByIdAsync(long id);
        Task<RequisitionDetailDto> CreateRequisitionAsync(CreateRequisitionDto dto);
        Task<List<FefoBatchSuggestionDto>> GetFefoSuggestionsAsync(long itemId, long warehouseId);
        Task<bool> DispatchRequisitionAsync(DispatchRequisitionDto dto);
        Task<bool> ReceiveLineItemAsync(ReceiveLineItemDto dto);
        Task<bool> ResolveDiscrepancyAsync(ResolveDiscrepancyDto dto);
        Task<List<RequisitionLifecycleAuditDto>> GetAuditLifecycleReportAsync(
            DateTime? startDate,
            DateTime? endDate,
            long? departmentId,
            long? itemId,
            string? status);
    }

    public class MaterialRequisitionService : IMaterialRequisitionService
    {
        private readonly AppDbContext _context;

        public MaterialRequisitionService(AppDbContext context)
        {
            _context = context;
        }

        // ========================================================
        // 1. القوائم المرجعية: حصر الصالات والمخازن المركزية والخامات
        // ========================================================
        public async Task<RequisitionLookupsDto> GetLookupsAsync()
        {
            var departments = await _context.Departments
                .AsNoTracking()
                .Where(d => (d.DepartmentName.Contains("قسم")
                          || d.DepartmentName.Contains("صالة")
                          || d.DepartmentName.Contains("تورت")
                          || d.DepartmentName.Contains("جاتوه")
                          || d.DepartmentName.Contains("شرقي")
                          || d.DepartmentName.Contains("اسبشيال")
                          || d.DepartmentName.Contains("مخبوزات")
                          || d.DepartmentName.Contains("كرواسون"))
                          && !d.DepartmentName.Contains("إدارة")
                          && !d.DepartmentName.Contains("العليا")
                          && !d.DepartmentName.Contains("المالية")
                          && !d.DepartmentName.Contains("الموارد"))
                .OrderBy(d => d.DepartmentName)
                .Select(d => new RequisitionDepartmentLookupDto
                {
                    Id = d.Id,
                    DepartmentName = d.DepartmentName
                })
                .ToListAsync();

            if (!departments.Any())
            {
                departments = await _context.Departments
                    .AsNoTracking()
                    .Where(d => !d.DepartmentName.Contains("إدارة") && !d.DepartmentName.Contains("المالية"))
                    .OrderBy(d => d.DepartmentName)
                    .Select(d => new RequisitionDepartmentLookupDto
                    {
                        Id = d.Id,
                        DepartmentName = d.DepartmentName
                    })
                    .ToListAsync();
            }

            var warehouses = await _context.Warehouses
                .AsNoTracking()
                .Where(w => w.Active &&
                            (w.WarehouseName.Contains("خامات")
                          || w.WarehouseName.Contains("رئيسي")
                          || w.WarehouseName.Contains("مستودع")
                          || w.WarehouseName.Contains("مركزي"))
                          && !w.WarehouseName.Contains("صالة")
                          && !w.WarehouseName.Contains("قسم")
                          && !w.WarehouseName.Contains("إدارة"))
                .OrderBy(w => w.WarehouseName)
                .Select(w => new RequisitionWarehouseLookupDto
                {
                    Id = w.Id,
                    WarehouseName = w.WarehouseName
                })
                .ToListAsync();

            if (!warehouses.Any())
            {
                warehouses = await _context.Warehouses
                    .AsNoTracking()
                    .Where(w => w.Active && !w.WarehouseName.Contains("صالة") && !w.WarehouseName.Contains("قسم"))
                    .OrderBy(w => w.WarehouseName)
                    .Select(w => new RequisitionWarehouseLookupDto
                    {
                        Id = w.Id,
                        WarehouseName = w.WarehouseName
                    })
                    .ToListAsync();
            }

            var rawMaterials = await _context.Items
                .AsNoTracking()
                .Where(i => i.Active && (i.ItemType.Contains("خام")
                                     || i.ItemType.Contains("وسيط")
                                     || i.ItemType.Contains("نصف")
                                     || i.ItemType.Contains("عجين")
                                     || i.ItemType == "RAW_MATERIAL"
                                     || i.ItemType == "WIP"))
                .OrderBy(i => i.ItemName)
                .Select(i => new RequisitionRawMaterialLookupDto
                {
                    Id = i.Id,
                    ItemCode = i.ItemCode,
                    ItemName = i.ItemName,
                    ItemType = i.ItemType,
                    Unit = i.Unit ?? "كجم"
                })
                .ToListAsync();

            return new RequisitionLookupsDto
            {
                Departments = departments,
                Warehouses = warehouses,
                RawMaterials = rawMaterials
            };
        }

        // ========================================================
        // 2. استعراض كافة طلبات الصرف
        // ========================================================
        public async Task<List<RequisitionDetailDto>> GetAllRequisitionsAsync(string? status = null)
        {
            var query = _context.MaterialRequisitions
                .Include(r => r.Department)
                .Include(r => r.FromWarehouse)
                .Include(r => r.Lines).ThenInclude(l => l.Item)
                .Include(r => r.Lines).ThenInclude(l => l.FromWarehouse)
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
                .Include(r => r.Lines).ThenInclude(l => l.FromWarehouse)
                .FirstOrDefaultAsync(r => r.Id == id);

            return req == null ? null : MapToDetailDto(req);
        }

        // ========================================================
        // 3. إنشاء طلب الصرف
        // ========================================================
        public async Task<RequisitionDetailDto> CreateRequisitionAsync(CreateRequisitionDto dto)
        {
            string reqNumber = $"MR-{DateTime.UtcNow:yyyyMMdd}-{new Random().Next(1000, 9999)}";

            long initialWarehouseId = dto.FromWarehouseId > 0
                ? dto.FromWarehouseId
                : (await _context.Warehouses.OrderBy(w => w.Id).Select(w => w.Id).FirstOrDefaultAsync());

            var req = new MaterialRequisition
            {
                RequisitionNumber = reqNumber,
                DepartmentId = dto.DepartmentId,
                FromWarehouseId = initialWarehouseId,
                Status = "SUBMITTED",
                Notes = dto.Notes,
                RequestedBy = dto.RequestedBy,
                CreatedAt = DateTime.UtcNow
            };

            foreach (var line in dto.Lines)
            {
                if (line.ItemId <= 0 || line.RequestedQty <= 0) continue;

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

        // ========================================================
        // 4. اقتراحات FEFO لتشغيلات الصرف (الخامات والمكونات الوسيطة)
        // ========================================================
        public async Task<List<FefoBatchSuggestionDto>> GetFefoSuggestionsAsync(long itemId, long warehouseId)
        {
            var receipts = await _context.StockMovements
                .AsNoTracking()
                .Where(m => m.ItemId == itemId && m.WarehouseId == warehouseId &&
                           (m.MovementType == "PURCHASE_RECEIPT" ||
                            m.MovementType == "PRODUCTION_RECEIPT" ||
                            m.MovementType == "PRODUCTION_IN" ||
                            m.MovementType == "INITIAL_STOCK" ||
                            m.MovementType == "STOCK_IN" ||
                            m.MovementType == "TRANSFER_IN"))
                .OrderBy(m => m.MovementAt)
                .ToListAsync();

            var suggestions = new List<FefoBatchSuggestionDto>();

            foreach (var r in receipts)
            {
                if (string.IsNullOrWhiteSpace(r.Notes)) continue;

                string batch = ExtractValue(r.Notes, "تشغيلة:", "|");
                if (string.IsNullOrWhiteSpace(batch)) batch = ExtractValue(r.Notes, "لوط:", "|");
                if (string.IsNullOrWhiteSpace(batch)) batch = ExtractValue(r.Notes, "LOT:", "|");
                if (string.IsNullOrWhiteSpace(batch)) batch = ExtractValue(r.Notes, "تشغيلة/تتبع:", "|");

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

        // ========================================================
        // 5. صرف الشحنة وتثبيت مخزن كل سطر خامة
        // ========================================================
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
                    line.FromWarehouseId = dl.WarehouseId > 0 ? dl.WarehouseId : req.FromWarehouseId;
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

        // ========================================================
        // 6. استلام السطر بالصالة
        // ========================================================
        public async Task<bool> ReceiveLineItemAsync(ReceiveLineItemDto dto)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var line = await _context.MaterialRequisitionLines
                    .Include(l => l.Requisition)
                        .ThenInclude(r => r!.Lines)
                    .FirstOrDefaultAsync(l => l.Id == dto.LineId);

                if (line == null || line.Requisition == null) return false;

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
                    line.Requisition.Status = "DISPUTED";
                }

                line.Requisition.ReceivedBy = dto.ReceivedBy;

                bool allCompleted = line.Requisition.Lines.All(l => l.Id == line.Id ? line.LineStatus == "ACCEPTED" : l.LineStatus == "ACCEPTED");

                if (allCompleted)
                {
                    await FinalizeInventoryTransferAsync(line.Requisition);
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
        // 7. فض النزاع وتسوية الفروق (محصنة ضد مسح المنصرف الأصلي)
        // ========================================================
        public async Task<bool> ResolveDiscrepancyAsync(ResolveDiscrepancyDto dto)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var line = await _context.MaterialRequisitionLines
                    .Include(l => l.Requisition)
                        .ThenInclude(r => r!.Lines)
                    .FirstOrDefaultAsync(l => l.Id == dto.LineId);

                if (line == null || line.Requisition == null) return false;

                // الاحتفاظ بالكمية المنصرفة الأصلية وتثبيت المستلم المعتمد لضمان دقة الرقابة
                line.ReceivedQty = dto.FinalApprovedQty;
                line.WarehouseNotes = dto.WarehouseResolutionNotes;
                line.LineStatus = "ACCEPTED";

                bool allCompleted = line.Requisition.Lines.All(l => l.Id == line.Id ? true : l.LineStatus == "ACCEPTED");

                if (allCompleted)
                {
                    await FinalizeInventoryTransferAsync(line.Requisition);
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
        // 8. محرك التقرير الرقابي الشامل واحتساب كشف الحساب التراكمي للصالات
        // ========================================================
        public async Task<List<RequisitionLifecycleAuditDto>> GetAuditLifecycleReportAsync(
            DateTime? startDate,
            DateTime? endDate,
            long? departmentId,
            long? itemId,
            string? status)
        {
            var allRequisitions = await GetAllRequisitionsAsync(null);
            if (allRequisitions == null || !allRequisitions.Any())
                return new List<RequisitionLifecycleAuditDto>();

            var allWarehouses = await _context.Warehouses.AsNoTracking().ToListAsync();
            var flatList = new List<RequisitionLifecycleAuditDto>();

            foreach (var req in allRequisitions)
            {
                var floorStore = allWarehouses.FirstOrDefault(w =>
                    !string.IsNullOrEmpty(req.DepartmentName) &&
                    (w.WarehouseName.Contains(req.DepartmentName) ||
                     (req.DepartmentName.Contains("شرقي") && w.WarehouseName.Contains("شرقي")) ||
                     (req.DepartmentName.Contains("جاتوه") && w.WarehouseName.Contains("جاتوه")) ||
                     (req.DepartmentName.Contains("تورت") && w.WarehouseName.Contains("تورت")) ||
                     (req.DepartmentName.Contains("اسبشيال") && w.WarehouseName.Contains("اسبشيال"))));

                string floorStoreName = floorStore?.WarehouseName ?? $"مخزن صالة {req.DepartmentName}";
                string centralStoreName = !string.IsNullOrEmpty(req.FromWarehouseName) ? req.FromWarehouseName : "المستودع الرئيسي";

                foreach (var line in req.Lines)
                {
                    decimal finalApproved = 0m;
                    decimal scrapLoss = 0m;
                    string disputeStatus = "مطابق بدون نزاع ✓";

                    if (line.LineStatus == "ACCEPTED")
                    {
                        finalApproved = line.ReceivedQty > 0 ? line.ReceivedQty : line.DispatchedQty;
                        scrapLoss = line.DispatchedQty > finalApproved ? (line.DispatchedQty - finalApproved) : 0m;
                        disputeStatus = scrapLoss > 0 ? "تم فض النزاع واعتماد التسوية ⚖️" : "مطابق بدون نزاع ✓";
                    }
                    else if (line.LineStatus == "DISPUTED")
                    {
                        finalApproved = line.ReceivedQty;
                        scrapLoss = line.DispatchedQty > line.ReceivedQty ? (line.DispatchedQty - line.ReceivedQty) : 0m;
                        disputeStatus = !string.IsNullOrWhiteSpace(line.WarehouseNotes)
                            ? "تم فض النزاع واعتماد التسوية ⚖️"
                            : "نزاع كميات معلق ⚠️";
                    }
                    else if (req.Status == "DISPATCHED" || line.LineStatus == "PENDING")
                    {
                        disputeStatus = "شحنة واردة قيد الفحص بالصالة";
                    }
                    else
                    {
                        disputeStatus = "بانتظار الصرف من المستودع";
                    }

                    flatList.Add(new RequisitionLifecycleAuditDto
                    {
                        RequisitionId = req.Id,
                        RequisitionNumber = req.RequisitionNumber,
                        RequestDate = req.CreatedAt,
                        DepartmentId = req.DepartmentId,
                        DepartmentName = req.DepartmentName,
                        RequestedBy = !string.IsNullOrWhiteSpace(req.RequestedBy) ? req.RequestedBy : "مشرف الصالة",
                        OverallStatus = req.Status,
                        ItemId = line.ItemId,
                        ItemCode = line.ItemCode,
                        ItemName = line.ItemName,
                        Unit = line.Unit,
                        RequestedQty = line.RequestedQty,
                        DispatchedBy = !string.IsNullOrWhiteSpace(req.DispatchedBy) ? req.DispatchedBy : "أمين المخزن",
                        DispatchedAt = req.DispatchedAt,
                        FromWarehouseName = centralStoreName,
                        DispatchedQty = line.DispatchedQty,
                        BatchNumber = !string.IsNullOrWhiteSpace(line.BatchNumber) ? line.BatchNumber : "-",
                        ExpiryDate = line.ExpiryDate,
                        ReceivedQty = line.ReceivedQty,
                        LineStatus = line.LineStatus,
                        DisputeStatusTitle = disputeStatus,
                        DiscrepancyReason = line.DiscrepancyReason,
                        FinalApprovedQty = finalApproved,
                        ScrapOrLossQty = scrapLoss,
                        ResolutionNotes = line.WarehouseNotes,
                        FloorStoreName = floorStoreName
                    });
                }
            }

            var processedRows = new List<RequisitionLifecycleAuditDto>();
            var groups = flatList.GroupBy(x => new { x.DepartmentId, x.ItemId });

            foreach (var grp in groups)
            {
                decimal runningFloorBalance = 0m;
                var sortedChronological = grp.OrderBy(x => x.RequestDate).ThenBy(x => x.RequisitionId).ToList();

                foreach (var row in sortedChronological)
                {
                    row.FloorStockBefore = Math.Round(runningFloorBalance, 2);

                    if (row.FinalApprovedQty > 0)
                    {
                        runningFloorBalance += row.FinalApprovedQty;
                    }

                    row.FloorStockAfter = Math.Round(runningFloorBalance, 2);
                }

                decimal currentTotalBalance = Math.Round(runningFloorBalance, 2);
                foreach (var row in sortedChronological)
                {
                    row.CurrentFloorStock = currentTotalBalance;
                    processedRows.Add(row);
                }
            }

            var filtered = processedRows.AsEnumerable();

            if (startDate.HasValue)
                filtered = filtered.Where(r => r.RequestDate.Date >= startDate.Value.Date);

            if (endDate.HasValue)
                filtered = filtered.Where(r => r.RequestDate.Date <= endDate.Value.Date);

            if (departmentId.HasValue && departmentId.Value > 0)
                filtered = filtered.Where(r => r.DepartmentId == departmentId.Value);

            if (itemId.HasValue && itemId.Value > 0)
                filtered = filtered.Where(r => r.ItemId == itemId.Value);

            if (!string.IsNullOrWhiteSpace(status))
                filtered = filtered.Where(r => r.OverallStatus.Equals(status, StringComparison.OrdinalIgnoreCase));

            return filtered.OrderByDescending(r => r.RequestDate).ThenByDescending(r => r.RequisitionId).ToList();
        }

        // ========================================================
        // 9. خصم الرصيد وترحيل حركة الصرف بمطابقة مادية تامة
        // ========================================================
        private async Task FinalizeInventoryTransferAsync(MaterialRequisition req)
        {
            req.Status = "COMPLETED";
            req.CompletedAt = DateTime.UtcNow;

            var lines = await _context.MaterialRequisitionLines
                .Include(l => l.Item)
                .Include(l => l.FromWarehouse)
                .Where(l => l.RequisitionId == req.Id)
                .ToListAsync();

            DateTime now = DateTime.UtcNow;

            foreach (var line in lines)
            {
                long actualWarehouseId = (line.FromWarehouseId.HasValue && line.FromWarehouseId.Value > 0)
                    ? line.FromWarehouseId.Value
                    : req.FromWarehouseId;

                // 1. خصم إجمالي الكمية التي خرجت بالفعل من المستودع
                decimal qtyLeftWarehouse = line.DispatchedQty > 0 ? line.DispatchedQty : line.ReceivedQty;

                var sourceBalance = await _context.StockBalances
                    .FirstOrDefaultAsync(b => b.ItemId == line.ItemId && b.WarehouseId == actualWarehouseId);

                if (sourceBalance != null)
                {
                    sourceBalance.Quantity -= qtyLeftWarehouse;
                    sourceBalance.AvailableQuantity = sourceBalance.Quantity - sourceBalance.ReservedQuantity;
                    sourceBalance.LastUpdatedAt = now;
                }
                else
                {
                    _context.StockBalances.Add(new StockBalance
                    {
                        CompanyId = (line.Item != null && line.Item.CompanyId > 0) ? line.Item.CompanyId : 1,
                        ItemId = line.ItemId,
                        WarehouseId = actualWarehouseId,
                        Quantity = -qtyLeftWarehouse,
                        AvailableQuantity = -qtyLeftWarehouse,
                        ReservedQuantity = 0,
                        LastUpdatedAt = now
                    });
                }

                long itemCompanyId = (line.Item != null && line.Item.CompanyId > 0) ? line.Item.CompanyId : 1;
                string whName = line.FromWarehouse?.WarehouseName ?? req.FromWarehouse?.WarehouseName ?? "مستودع";

                // 2. إثبات صرف ما استلمته صالة التشغيل فعلياً
                _context.StockMovements.Add(new StockMovement
                {
                    CompanyId = itemCompanyId,
                    ItemId = line.ItemId,
                    WarehouseId = actualWarehouseId,
                    MovementType = "PRODUCTION_ISSUE",
                    Quantity = line.ReceivedQty,
                    UnitCost = line.UnitCost,
                    ReferenceType = "MATERIAL_REQUISITION",
                    ReferenceId = req.Id,
                    MovementAt = now,
                    CreatedAt = now,
                    Notes = $"صرف لصالة التشغيل | مخزن: {whName} | تشغيلة: {line.BatchNumber} | صلاحية: {line.ExpiryDate:yyyy-MM-dd} | إذن رقم: {req.RequisitionNumber}"
                });

                // 3. إثبات الهالك أو العجز كحركة مستقلة في كارت الصنف إن وجد
                decimal shortage = qtyLeftWarehouse - line.ReceivedQty;
                if (shortage > 0)
                {
                    _context.StockMovements.Add(new StockMovement
                    {
                        CompanyId = itemCompanyId,
                        ItemId = line.ItemId,
                        WarehouseId = actualWarehouseId,
                        MovementType = "TRANSFER_LOSS",
                        Quantity = shortage,
                        UnitCost = line.UnitCost,
                        ReferenceType = "MATERIAL_REQUISITION",
                        ReferenceId = req.Id,
                        MovementAt = now,
                        CreatedAt = now,
                        Notes = $"عجز / هدر نقل خامات معتمد | إذن: {req.RequisitionNumber} | {line.WarehouseNotes}"
                    });
                }
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
                    ItemCode = l.Item?.ItemCode ?? string.Empty,
                    ItemName = l.Item?.ItemName ?? "صنف",
                    Unit = l.Item?.Unit ?? string.Empty,
                    WarehouseId = l.FromWarehouseId ?? r.FromWarehouseId,
                    WarehouseName = l.FromWarehouse?.WarehouseName ?? r.FromWarehouse?.WarehouseName ?? "المخزن الرئيسي",
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

        private static string ExtractValue(string text, string key, string separator)
        {
            if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(key)) return string.Empty;

            int startIdx = text.IndexOf(key, StringComparison.OrdinalIgnoreCase);
            if (startIdx == -1) return string.Empty;

            startIdx += key.Length;
            if (startIdx >= text.Length) return string.Empty;

            int endIdx = text.IndexOf(separator, startIdx, StringComparison.OrdinalIgnoreCase);
            if (endIdx == -1) endIdx = text.Length;

            return text.Substring(startIdx, endIdx - startIdx).Trim();
        }
    }
}