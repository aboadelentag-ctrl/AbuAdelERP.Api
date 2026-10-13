using Microsoft.EntityFrameworkCore;
using KayanERP.Api.Models;
using KayanERP.Api.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KayanERP.Api.Services
{
    public interface IVendorQcService
    {
        Task<List<VendorQcScoreDto>> GetVendorRatingsAsync(DateTime? fromDate = null, DateTime? toDate = null);
        Task<List<QcInspectionHistoryDto>> GetInspectionHistoryAsync(DateTime? fromDate = null, DateTime? toDate = null);
    }

    public class VendorQcService : IVendorQcService
    {
        private readonly AppDbContext _context;

        public VendorQcService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<VendorQcScoreDto>> GetVendorRatingsAsync(DateTime? fromDate = null, DateTime? toDate = null)
        {
            var query = _context.StockMovements
                .Where(m => m.ReferenceType == "PURCHASE_ORDER" &&
                            m.ReferenceId.HasValue &&
                           (m.MovementType == "PURCHASE_RECEIPT" || m.MovementType == "QC_REJECTED"));

            if (fromDate.HasValue)
            {
                var fromUtc = DateTime.SpecifyKind(fromDate.Value.Date, DateTimeKind.Utc);
                query = query.Where(m => m.MovementAt >= fromUtc);
            }

            if (toDate.HasValue)
            {
                var toUtc = DateTime.SpecifyKind(toDate.Value.Date.AddDays(1).AddTicks(-1), DateTimeKind.Utc);
                query = query.Where(m => m.MovementAt <= toUtc);
            }

            var movements = await query.ToListAsync();

            var poIds = movements
                .Where(m => m.ReferenceId.HasValue)
                .Select(m => m.ReferenceId!.Value)
                .Distinct()
                .ToList();

            var poSupplierMap = await _context.PurchaseOrders
                .Where(p => poIds.Contains(p.Id) && p.SupplierId > 0)
                .ToDictionaryAsync(p => p.Id, p => p.SupplierId);

            var suppliers = await _context.Suppliers.ToDictionaryAsync(s => s.Id, s => s.SupplierName);

            var ratings = new List<VendorQcScoreDto>();

            var validMovements = movements
                .Where(m => m.ReferenceId.HasValue && poSupplierMap.ContainsKey(m.ReferenceId.Value))
                .ToList();

            var groupedBySupplier = validMovements.GroupBy(m => poSupplierMap[m.ReferenceId!.Value]);

            foreach (var group in groupedBySupplier)
            {
                long supplierId = group.Key;
                string supplierName = suppliers.ContainsKey(supplierId) ? suppliers[supplierId] : "مورد غير معرف";

                decimal acceptedQty = group.Where(m => m.MovementType == "PURCHASE_RECEIPT").Sum(m => m.Quantity);
                decimal rejectedQty = group.Where(m => m.MovementType == "QC_REJECTED").Sum(m => m.Quantity);
                decimal totalQty = acceptedQty + rejectedQty;

                decimal rate = totalQty > 0 ? Math.Round((acceptedQty / totalQty) * 100, 2) : 100m;

                string grade = "A";
                if (rate < 85m) grade = "C";
                else if (rate < 95m) grade = "B";

                int rejectIncidents = group.Count(m => m.MovementType == "QC_REJECTED");
                int totalShipments = group.Select(m => m.ReferenceId!.Value).Distinct().Count();

                ratings.Add(new VendorQcScoreDto
                {
                    SupplierId = supplierId,
                    SupplierName = supplierName,
                    TotalShipments = totalShipments,
                    TotalReceivedQty = totalQty,
                    TotalAcceptedQty = acceptedQty,
                    TotalRejectedQty = rejectedQty,
                    AcceptanceRate = rate,
                    QualityGrade = grade,
                    RejectionIncidentsCount = rejectIncidents
                });
            }

            return ratings.OrderByDescending(r => r.AcceptanceRate).ToList();
        }

        public async Task<List<QcInspectionHistoryDto>> GetInspectionHistoryAsync(DateTime? fromDate = null, DateTime? toDate = null)
        {
            var query = _context.StockMovements
                .Where(m => m.ReferenceType == "PURCHASE_ORDER" &&
                            m.ReferenceId.HasValue &&
                           (m.MovementType == "PURCHASE_RECEIPT" || m.MovementType == "QC_REJECTED"));

            if (fromDate.HasValue)
            {
                var fromUtc = DateTime.SpecifyKind(fromDate.Value.Date, DateTimeKind.Utc);
                query = query.Where(m => m.MovementAt >= fromUtc);
            }

            if (toDate.HasValue)
            {
                var toUtc = DateTime.SpecifyKind(toDate.Value.Date.AddDays(1).AddTicks(-1), DateTimeKind.Utc);
                query = query.Where(m => m.MovementAt <= toUtc);
            }

            var movements = await query
                .OrderByDescending(m => m.Id)
                .Take(200)
                .ToListAsync();

            var poIds = movements
                .Where(m => m.ReferenceId.HasValue)
                .Select(m => m.ReferenceId!.Value)
                .Distinct()
                .ToList();

            var itemIds = movements.Select(m => m.ItemId).Distinct().ToList();

            var poSupplierMap = await _context.PurchaseOrders
                .Where(p => poIds.Contains(p.Id) && p.SupplierId > 0)
                .ToDictionaryAsync(p => p.Id, p => p.SupplierId);

            var suppliers = await _context.Suppliers.ToDictionaryAsync(s => s.Id, s => s.SupplierName);
            var items = await _context.Items
                .Where(i => itemIds.Contains(i.Id))
                .ToDictionaryAsync(i => i.Id, i => new { i.ItemCode, i.ItemName });

            var result = new List<QcInspectionHistoryDto>();

            foreach (var m in movements)
            {
                long refId = m.ReferenceId ?? 0L;
                long supId = (refId > 0 && poSupplierMap.ContainsKey(refId)) ? poSupplierMap[refId] : 0L;

                string supName = (supId > 0 && suppliers.ContainsKey(supId)) ? suppliers[supId] : "مورد غير محدد";

                string itmCode = items.ContainsKey(m.ItemId) ? items[m.ItemId].ItemCode : "";
                string itmName = items.ContainsKey(m.ItemId) ? items[m.ItemId].ItemName : "صنف";

                result.Add(new QcInspectionHistoryDto
                {
                    MovementId = m.Id,
                    InspectionDate = m.MovementAt,
                    SupplierId = supId,
                    SupplierName = supName,
                    ItemId = m.ItemId,
                    ItemCode = itmCode,
                    ItemName = itmName,
                    MovementType = m.MovementType,
                    Quantity = m.Quantity,
                    Notes = m.Notes ?? string.Empty
                });
            }

            return result;
        }
    }
}