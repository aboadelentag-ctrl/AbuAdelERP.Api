using Microsoft.EntityFrameworkCore;
using AbuAdelERP.Api.Models;
using AbuAdelERP.Api.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AbuAdelERP.Api.Services
{
    public interface IStockCardService
    {
        Task<ItemStockCardDetailDto?> GetItemStockCardAsync(long itemId, long warehouseId, DateTime? fromDate = null, DateTime? toDate = null);
        Task<List<ItemStockSummaryDto>> GetWarehouseInventoryAsync(long warehouseId);
    }

    public class StockCardService : IStockCardService
    {
        private readonly AppDbContext _context;

        public StockCardService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<ItemStockSummaryDto>> GetWarehouseInventoryAsync(long warehouseId)
        {
            var balances = await _context.StockBalances
                .Where(b => warehouseId == 0 || b.WarehouseId == warehouseId)
                .ToListAsync();

            var itemIds = balances.Select(b => b.ItemId).Distinct().ToList();
            var warehouseIds = balances.Select(b => b.WarehouseId).Distinct().ToList();

            var items = await _context.Items
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
                .Where(w => warehouseIds.Contains(w.Id))
                .ToDictionaryAsync(w => w.Id, w => w.WarehouseName);

            var list = new List<ItemStockSummaryDto>();

            foreach (var b in balances)
            {
                string itmCode = items.ContainsKey(b.ItemId) ? items[b.ItemId].ItemCode : "";
                string itmName = items.ContainsKey(b.ItemId) ? items[b.ItemId].ItemName : "صنف";
                string unit = items.ContainsKey(b.ItemId) ? items[b.ItemId].Unit : "";
                bool hasExp = items.ContainsKey(b.ItemId) && items[b.ItemId].HasExpiryDate;
                decimal rLevel = items.ContainsKey(b.ItemId) ? items[b.ItemId].ReorderLevel : 0;
                decimal sStock = items.ContainsKey(b.ItemId) ? items[b.ItemId].SafetyStock : 0;
                decimal mStock = items.ContainsKey(b.ItemId) ? items[b.ItemId].MaxStock : 0;
                string wName = warehouses.ContainsKey(b.WarehouseId) ? warehouses[b.WarehouseId] : "مخزن";

                decimal totalQty = Convert.ToDecimal(b.Quantity);
                decimal availQty = Convert.ToDecimal(b.AvailableQuantity);
                decimal resQty = Convert.ToDecimal(b.ReservedQuantity);

                list.Add(new ItemStockSummaryDto
                {
                    ItemId = b.ItemId,
                    ItemCode = itmCode,
                    ItemName = itmName,
                    Unit = unit,
                    WarehouseId = b.WarehouseId,
                    WarehouseName = wName,
                    TotalQuantity = totalQty,
                    AvailableQuantity = availQty,
                    ReservedQuantity = resQty,
                    HasExpiryDate = hasExp,
                    ReorderLevel = rLevel,
                    SafetyStock = sStock,
                    MaxStock = mStock
                });
            }

            return list.OrderBy(x => x.ItemName).ToList();
        }

        public async Task<ItemStockCardDetailDto?> GetItemStockCardAsync(long itemId, long warehouseId, DateTime? fromDate = null, DateTime? toDate = null)
        {
            var item = await _context.Items.FirstOrDefaultAsync(i => i.Id == itemId);
            if (item == null) return null;

            var warehouse = await _context.Warehouses.FirstOrDefaultAsync(w => w.Id == warehouseId);
            string warehouseName = warehouse?.WarehouseName ?? "كافة المخازن (رصيد مجمع)";

            // 1. حساب الأرصدة الحالية
            var balancesQuery = _context.StockBalances.Where(b => b.ItemId == itemId);
            if (warehouseId > 0)
            {
                balancesQuery = balancesQuery.Where(b => b.WarehouseId == warehouseId);
            }

            decimal currentTotalStock = await balancesQuery.SumAsync(b => (decimal?)b.Quantity) ?? 0m;
            decimal currentAvailStock = await balancesQuery.SumAsync(b => (decimal?)b.AvailableQuantity) ?? currentTotalStock;
            decimal currentResStock = await balancesQuery.SumAsync(b => (decimal?)b.ReservedQuantity) ?? 0m;

            // 2. جلب حركات الصنف
            var movementsQuery = _context.StockMovements.Where(m => m.ItemId == itemId);
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

            // قائمة الحركات المعتمدة كوارد
            var incomingTypes = new HashSet<string>
            {
                "PURCHASE_RECEIPT",
                "PRODUCTION_IN",
                "INITIAL_STOCK",
                "STOCK_IN",
                "TRANSFER_IN",
                "FG_TRANSFER_IN" // تم التضمين رسمياً
            };

            // 3. حساب رصيد أول المدة
            decimal openingBalance = 0;
            if (fromUtc.HasValue)
            {
                var priorMovements = allMovements.Where(m => m.MovementAt < fromUtc.Value).ToList();
                foreach (var pm in priorMovements)
                {
                    decimal qty = Math.Abs(Convert.ToDecimal(pm.Quantity));
                    if (incomingTypes.Contains(pm.MovementType))
                    {
                        openingBalance += qty;
                    }
                    else if (pm.MovementType != "QC_REJECTED")
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

            // 4. تصفية حركات الفترة واحتساب الوارد والمنصرف بدقة
            var filteredMovements = allMovements.AsEnumerable();
            if (fromUtc.HasValue) filteredMovements = filteredMovements.Where(m => m.MovementAt >= fromUtc.Value);
            if (toUtc.HasValue) filteredMovements = filteredMovements.Where(m => m.MovementAt <= toUtc.Value);

            foreach (var m in filteredMovements)
            {
                decimal inQty = 0;
                decimal outQty = 0;
                decimal mQty = Math.Abs(Convert.ToDecimal(m.Quantity));

                if (incomingTypes.Contains(m.MovementType))
                {
                    inQty = mQty;
                    running += inQty;
                }
                else if (m.MovementType == "QC_REJECTED")
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
                    ReferenceType = m.ReferenceType ?? "",
                    ReferenceId = m.ReferenceId,
                    Notes = m.Notes ?? ""
                });
            }

            // 5. تحليل التشغيلات الحية (مشتريات + إنتاج الصالة + استلام الكنترول)
            var receiptMovements = allMovements.Where(m =>
                m.MovementType == "PURCHASE_RECEIPT" ||
                m.MovementType == "PRODUCTION_IN" ||
                m.MovementType == "FG_TRANSFER_IN");

            if (fromUtc.HasValue) receiptMovements = receiptMovements.Where(m => m.MovementAt >= fromUtc.Value);
            if (toUtc.HasValue) receiptMovements = receiptMovements.Where(m => m.MovementAt <= toUtc.Value);

            var batchMap = new Dictionary<string, decimal>();
            var expiryMap = new Dictionary<string, string>();

            foreach (var rm in receiptMovements)
            {
                if (string.IsNullOrWhiteSpace(rm.Notes)) continue;

                string batchNo = ExtractValue(rm.Notes, "تشغيلة:", "|");
                if (string.IsNullOrWhiteSpace(batchNo))
                {
                    batchNo = ExtractValue(rm.Notes, "تشغيلة/تتبع:", "|");
                }

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

            // مطابقة الرصيد التراكمي اللحظي
            decimal finalTotal = balancesQuery.Any() ? currentTotalStock : running;
            decimal finalAvail = balancesQuery.Any() ? currentAvailStock : running;
            decimal finalRes = currentResStock;

            // 6. مراقبة حدود المخزون ونقاط إعادة الطلب
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
                    statusMsg = $"⚠️ تحذير حرج: كسر حد الأمان ({safetyStock:N2} {item.Unit})! يجب التوريد فوراً لتفادي توقف الإنتاج.";
                }
                else if (reorderLevel > 0 && finalAvail <= reorderLevel)
                {
                    stockStatus = "ReorderNeeded";
                    statusMsg = $"🔔 تنبيه: وصل المخزون لنقطة إعادة الطلب ({reorderLevel:N2} {item.Unit}). يوصى ببدء إصدار أمر شراء.";
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

        private string ExtractValue(string text, string key, string separator)
        {
            int startIdx = text.IndexOf(key, StringComparison.OrdinalIgnoreCase);
            if (startIdx == -1) return "";
            startIdx += key.Length;

            int endIdx = text.IndexOf(separator, startIdx, StringComparison.OrdinalIgnoreCase);
            if (endIdx == -1) endIdx = text.Length;

            return text.Substring(startIdx, endIdx - startIdx).Trim();
        }
    }
}