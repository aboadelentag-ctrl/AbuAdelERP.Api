using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using KayanERP.Api.Models;
using KayanERP.Shared.DTOs;

namespace KayanERP.Api.Services
{
    public interface IStockCardService
    {
        Task<ItemStockCardDetailDto?> GetItemStockCardAsync(long itemId, long warehouseId, DateTime? fromDate = null, DateTime? toDate = null);
        Task<List<ItemStockSummaryDto>> GetWarehouseInventoryAsync(long warehouseId);
    }

    public class StockCardService : IStockCardService
    {
        private readonly AppDbContext _context;

        // قائمة الحركات المعتمدة رسمياً كوارد لكافة العمليات
        private static readonly HashSet<string> IncomingTypes = new(StringComparer.OrdinalIgnoreCase)
        {
            "PURCHASE_RECEIPT",
            "PRODUCTION_IN",
            "PRODUCTION_RECEIPT",
            "INITIAL_STOCK",
            "STOCK_IN",
            "TRANSFER_IN",
            "FG_TRANSFER_IN",
            "ADJUSTMENT_IN"
        };

        public StockCardService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<ItemStockSummaryDto>> GetWarehouseInventoryAsync(long warehouseId)
        {
            var balancesQuery = _context.StockBalances.AsNoTracking();
            if (warehouseId > 0)
            {
                balancesQuery = balancesQuery.Where(b => b.WarehouseId == warehouseId);
            }

            var balances = await balancesQuery.ToListAsync();
            if (!balances.Any()) return new List<ItemStockSummaryDto>();

            var itemIds = balances.Select(b => b.ItemId).Distinct().ToList();
            var warehouseIds = balances.Select(b => b.WarehouseId).Distinct().ToList();

            var items = await _context.Items
                .AsNoTracking()
                .Where(i => itemIds.Contains(i.Id))
                .ToDictionaryAsync(i => i.Id, i => new
                {
                    i.ItemCode,
                    i.ItemName,
                    i.Unit,
                    i.HasExpiryDate,
                    i.ReorderLevel,
                    i.SafetyStock,
                    i.MaxStock
                });

            var warehouses = await _context.Warehouses
                .AsNoTracking()
                .Where(w => warehouseIds.Contains(w.Id))
                .ToDictionaryAsync(w => w.Id, w => w.WarehouseName);

            var list = new List<ItemStockSummaryDto>(balances.Count);

            foreach (var b in balances)
            {
                items.TryGetValue(b.ItemId, out var itm);
                warehouses.TryGetValue(b.WarehouseId, out var wName);

                list.Add(new ItemStockSummaryDto
                {
                    ItemId = b.ItemId,
                    ItemCode = itm?.ItemCode ?? string.Empty,
                    ItemName = itm?.ItemName ?? "صنف غير معروف",
                    Unit = itm?.Unit ?? string.Empty,
                    WarehouseId = b.WarehouseId,
                    WarehouseName = wName ?? "مخزن",
                    TotalQuantity = Convert.ToDecimal(b.Quantity),
                    AvailableQuantity = Convert.ToDecimal(b.AvailableQuantity),
                    ReservedQuantity = Convert.ToDecimal(b.ReservedQuantity),
                    HasExpiryDate = itm?.HasExpiryDate ?? false,
                    ReorderLevel = itm?.ReorderLevel ?? 0,
                    SafetyStock = itm?.SafetyStock ?? 0,
                    MaxStock = itm?.MaxStock ?? 0
                });
            }

            return list.OrderBy(x => x.ItemName).ToList();
        }

        public async Task<ItemStockCardDetailDto?> GetItemStockCardAsync(long itemId, long warehouseId, DateTime? fromDate = null, DateTime? toDate = null)
        {
            var item = await _context.Items
                .AsNoTracking()
                .FirstOrDefaultAsync(i => i.Id == itemId);

            if (item == null) return null;

            var warehouse = warehouseId > 0
                ? await _context.Warehouses.AsNoTracking().FirstOrDefaultAsync(w => w.Id == warehouseId)
                : null;

            string warehouseName = warehouse?.WarehouseName ?? "كافة المخازن (رصيد مجمع)";

            var balancesQuery = _context.StockBalances.AsNoTracking().Where(b => b.ItemId == itemId);
            if (warehouseId > 0)
            {
                balancesQuery = balancesQuery.Where(b => b.WarehouseId == warehouseId);
            }

            decimal currentTotalStock = await balancesQuery.SumAsync(b => (decimal?)b.Quantity) ?? 0m;
            decimal currentAvailStock = await balancesQuery.SumAsync(b => (decimal?)b.AvailableQuantity) ?? currentTotalStock;
            decimal currentResStock = await balancesQuery.SumAsync(b => (decimal?)b.ReservedQuantity) ?? 0m;

            var movementsQuery = _context.StockMovements.AsNoTracking().Where(m => m.ItemId == itemId);
            if (warehouseId > 0)
            {
                movementsQuery = movementsQuery.Where(m => m.WarehouseId == warehouseId);
            }

            var allMovements = await movementsQuery
                .OrderBy(m => m.MovementAt)
                .ThenBy(m => m.Id)
                .ToListAsync();

            DateTime? fromUtc = fromDate.HasValue ? DateTime.SpecifyKind(fromDate.Value.Date, DateTimeKind.Utc) : null;
            DateTime? toUtc = toDate.HasValue ? DateTime.SpecifyKind(toDate.Value.Date.AddDays(1).AddTicks(-1), DateTimeKind.Utc) : null;

            decimal running = 0;
            var ledger = new List<StockMovementLedgerDto>();

            // حساب رصيد أول المدة
            decimal openingBalance = 0;
            if (fromUtc.HasValue)
            {
                var priorMovements = allMovements.Where(m => m.MovementAt < fromUtc.Value);
                foreach (var pm in priorMovements)
                {
                    decimal qty = Math.Abs(Convert.ToDecimal(pm.Quantity));
                    if (IncomingTypes.Contains(pm.MovementType))
                    {
                        openingBalance += qty;
                    }
                    else if (!string.Equals(pm.MovementType, "QC_REJECTED", StringComparison.OrdinalIgnoreCase))
                    {
                        openingBalance -= qty;
                    }
                }

                running = openingBalance;

                ledger.Add(new StockMovementLedgerDto
                {
                    MovementId = 0,
                    MovementAt = fromUtc.Value,
                    MovementType = "OPENING_BALANCE",
                    InQuantity = 0,
                    OutQuantity = 0,
                    RunningBalance = openingBalance,
                    UnitCost = 0,
                    ReferenceType = "SYSTEM",
                    ReferenceId = null,
                    Notes = $"رصيد افتتاحي ما قبل تاريخ {fromDate:yyyy-MM-dd}"
                });
            }

            var filteredMovements = allMovements.AsEnumerable();
            if (fromUtc.HasValue) filteredMovements = filteredMovements.Where(m => m.MovementAt >= fromUtc.Value);
            if (toUtc.HasValue) filteredMovements = filteredMovements.Where(m => m.MovementAt <= toUtc.Value);

            foreach (var m in filteredMovements)
            {
                decimal inQty = 0;
                decimal outQty = 0;
                decimal mQty = Math.Abs(Convert.ToDecimal(m.Quantity));

                if (IncomingTypes.Contains(m.MovementType))
                {
                    inQty = mQty;
                    running += inQty;
                }
                else if (string.Equals(m.MovementType, "QC_REJECTED", StringComparison.OrdinalIgnoreCase))
                {
                    inQty = 0;
                    outQty = 0;
                }
                else
                {
                    outQty = mQty;
                    running -= outQty;
                }

                ledger.Add(new StockMovementLedgerDto
                {
                    MovementId = m.Id,
                    MovementAt = m.MovementAt,
                    MovementType = m.MovementType,
                    InQuantity = inQty,
                    OutQuantity = outQty,
                    RunningBalance = running,
                    UnitCost = Convert.ToDecimal(m.UnitCost),
                    ReferenceType = m.ReferenceType ?? string.Empty,
                    ReferenceId = m.ReferenceId,
                    Notes = m.Notes ?? string.Empty
                });
            }

            // تحليل تتبع اللوطات
            var receiptMovements = allMovements.Where(m =>
                string.Equals(m.MovementType, "PURCHASE_RECEIPT", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(m.MovementType, "PRODUCTION_IN", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(m.MovementType, "PRODUCTION_RECEIPT", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(m.MovementType, "FG_TRANSFER_IN", StringComparison.OrdinalIgnoreCase));

            if (fromUtc.HasValue) receiptMovements = receiptMovements.Where(m => m.MovementAt >= fromUtc.Value);
            if (toUtc.HasValue) receiptMovements = receiptMovements.Where(m => m.MovementAt <= toUtc.Value);

            var batchMap = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
            var expiryMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            foreach (var rm in receiptMovements)
            {
                if (string.IsNullOrWhiteSpace(rm.Notes)) continue;

                string batchNo = ExtractValue(rm.Notes, "تشغيلة:", "|");
                if (string.IsNullOrWhiteSpace(batchNo)) batchNo = ExtractValue(rm.Notes, "لوط:", "|");
                if (string.IsNullOrWhiteSpace(batchNo)) batchNo = ExtractValue(rm.Notes, "LOT:", "|");
                if (string.IsNullOrWhiteSpace(batchNo)) batchNo = ExtractValue(rm.Notes, "تشغيلة/تتبع:", "|");
                if (string.IsNullOrWhiteSpace(batchNo)) batchNo = $"تشغيلة-{rm.Id}";
                batchNo = batchNo.Trim();

                string expDate = ExtractValue(rm.Notes, "صلاحية:", "|").Trim();

                if (!batchMap.ContainsKey(batchNo))
                {
                    batchMap[batchNo] = 0;
                    expiryMap[batchNo] = expDate;
                }
                batchMap[batchNo] += Math.Abs(Convert.ToDecimal(rm.Quantity));
            }

            var batchesList = new List<ItemBatchStockDto>();
            var today = DateTime.Today;

            foreach (var kvp in batchMap)
            {
                string expStr = expiryMap[kvp.Key];
                int? daysRemaining = null;
                string status = "Valid";

                if (DateTime.TryParse(expStr, out DateTime expDateVal))
                {
                    daysRemaining = (expDateVal.Date - today).Days;
                    if (daysRemaining < 0) status = "Expired";
                    else if (daysRemaining <= 60) status = "Expiring_Soon";
                }

                batchesList.Add(new ItemBatchStockDto
                {
                    BatchNumber = kvp.Key,
                    Quantity = kvp.Value,
                    ExpiryDate = !string.IsNullOrWhiteSpace(expStr) ? expStr : "غير محدد",
                    DaysRemaining = daysRemaining,
                    Status = status
                });
            }

            decimal finalTotal = balancesQuery.Any() ? currentTotalStock : running;
            decimal finalAvail = balancesQuery.Any() ? currentAvailStock : running;
            decimal finalRes = currentResStock;

            decimal reorderLevel = item.ReorderLevel;
            decimal safetyStock = item.SafetyStock;
            decimal maxStock = item.MaxStock;

            string stockStatus = "Safe";
            string statusMsg = "الرصيد في النطاق الآمن والمستقر";

            if (reorderLevel > 0 || safetyStock > 0)
            {
                if (safetyStock > 0 && finalAvail <= safetyStock)
                {
                    stockStatus = "Critical";
                    statusMsg = $"⚠️ تحذير حرج: كسر حد الأمان ({safetyStock:N2} {item.Unit})! يجب التوريد فوراً.";
                }
                else if (reorderLevel > 0 && finalAvail <= reorderLevel)
                {
                    stockStatus = "ReorderNeeded";
                    statusMsg = $"🔔 تنبيه: وصل المخزون لنقطة إعادة الطلب ({reorderLevel:N2} {item.Unit}).";
                }
            }
            else
            {
                statusMsg = "لم يتم تحديد حدود دنيا أو أمان لبطاقة هذا الصنف.";
            }

            return new ItemStockCardDetailDto
            {
                Summary = new ItemStockSummaryDto
                {
                    ItemId = item.Id,
                    ItemCode = item.ItemCode,
                    ItemName = item.ItemName,
                    Unit = item.Unit,
                    WarehouseId = warehouseId,
                    WarehouseName = warehouseName,
                    TotalQuantity = finalTotal,
                    AvailableQuantity = finalAvail,
                    ReservedQuantity = finalRes,
                    HasExpiryDate = item.HasExpiryDate,
                    ReorderLevel = reorderLevel,
                    SafetyStock = safetyStock,
                    MaxStock = maxStock,
                    StockStatus = stockStatus,
                    StatusMessage = statusMsg
                },
                Batches = batchesList.OrderBy(b => b.DaysRemaining ?? 9999).ToList(),
                Ledger = ledger.OrderByDescending(l => l.MovementAt).ThenByDescending(l => l.MovementId).ToList()
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