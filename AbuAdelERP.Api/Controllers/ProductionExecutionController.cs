using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AbuAdelERP.Api.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AbuAdelERP.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductionExecutionController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ProductionExecutionController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet("pending-orders")]
        public async Task<IActionResult> GetPendingOrders()
        {
            var pendingOrders = await _context.ProductionOrders
                .Where(o => o.Status != "COMPLETED" && o.Status != "مكتمل")
                .Select(o => new PendingOrderDto
                {
                    OrderId = o.Id,
                    OrderNumber = o.OrderNumber,
                    OrderDate = o.OrderDate,
                    Status = o.Status,
                    Notes = o.Notes
                })
                .OrderBy(o => o.OrderDate)
                .ToListAsync();

            return Ok(pendingOrders);
        }

        [HttpGet("execution-template/{orderId}")]
        public async Task<IActionResult> GetExecutionTemplate(long orderId)
        {
            var order = await _context.ProductionOrders.FindAsync(orderId);
            if (order == null) return NotFound("أمر التشغيل غير موجود.");

            var orderDetails = await _context.ProductionOrderDetails
                .Include(d => d.Item)
                .Include(d => d.Recipe)
                    .ThenInclude(r => r.RecipeLines)
                        .ThenInclude(l => l.ComponentItem)
                .Where(d => d.ProductionOrderId == orderId && d.Status != "COMPLETED" && d.Status != "مكتمل")
                .ToListAsync();

            if (!orderDetails.Any())
                return NotFound("لم يتم العثور على تفاصيل لهذا الأمر أو أنه مقفل مسبقاً.");

            // استخراج معرفات المكونات لجلب آخر تكلفة مسجلة لها
            var componentIds = orderDetails
                .Where(d => d.Recipe != null)
                .SelectMany(d => d.Recipe!.RecipeLines.Select(l => l.ComponentItemId))
                .Distinct()
                .ToList();

            var latestCosts = await _context.StockMovements
                .Where(m => componentIds.Contains(m.ItemId) && m.UnitCost > 0)
                .GroupBy(m => m.ItemId)
                .Select(g => new
                {
                    ItemId = g.Key,
                    Cost = g.OrderByDescending(x => x.MovementAt).Select(x => x.UnitCost).FirstOrDefault()
                })
                .ToDictionaryAsync(x => x.ItemId, x => x.Cost);

            var executionTemplate = new List<OrderExecutionDetailDto>();

            foreach (var detail in orderDetails)
            {
                // معالجة التحويل الآمن من int? إلى int وتفادي خطأ CS0266
                int shelfLife = (detail.Item?.ShelfLifeDays.HasValue == true && detail.Item.ShelfLifeDays.Value > 0)
                    ? detail.Item.ShelfLifeDays.Value
                    : 30;

                var expiryDate = DateTime.UtcNow.Date.AddDays(shelfLife);
                string generatedBatch = $"LOT-PRD-{DateTime.UtcNow:yyyyMMdd}-{order.OrderNumber}-{detail.ItemId}";

                var execDetail = new OrderExecutionDetailDto
                {
                    OrderDetailId = detail.Id,
                    ProductItemId = detail.ItemId,
                    ProductName = detail.Item?.ItemName ?? "منتج تام",
                    RecipeId = detail.RecipeId,
                    PlannedQuantity = detail.PlannedQuantity,
                    ProducedQuantity = detail.PlannedQuantity,
                    BatchNumber = generatedBatch,
                    ExpiryDate = expiryDate,
                    Materials = new List<ExecutionMaterialDto>()
                };

                if (detail.Recipe != null && detail.Recipe.YieldQuantity > 0)
                {
                    decimal ratio = detail.PlannedQuantity / detail.Recipe.YieldQuantity;

                    foreach (var line in detail.Recipe.RecipeLines)
                    {
                        latestCosts.TryGetValue(line.ComponentItemId, out decimal unitCost);
                        if (unitCost <= 0) unitCost = 5.0m;

                        execDetail.Materials.Add(new ExecutionMaterialDto
                        {
                            ComponentItemId = line.ComponentItemId,
                            ComponentName = line.ComponentItem?.ItemName ?? "خامة",
                            RequiredQuantity = Math.Round(line.Quantity * ratio, 3),
                            ActualConsumedQuantity = Math.Round(line.Quantity * ratio, 3),
                            Unit = line.Unit ?? "كجم",
                            UnitCost = unitCost
                        });
                    }
                }
                executionTemplate.Add(execDetail);
            }

            return Ok(executionTemplate);
        }

        [HttpPost("execute")]
        public async Task<IActionResult> ExecuteOrder([FromBody] ExecuteOrderRequestDto request)
        {
            var order = await _context.ProductionOrders.FindAsync(request.OrderId);
            if (order == null) return NotFound("أمر التشغيل غير موجود.");
            if (order.Status == "COMPLETED" || order.Status == "مكتمل") return BadRequest("هذا الأمر تم تقفيله مسبقاً.");

            long companyId = order.CompanyId > 0 ? order.CompanyId : 1;
            long warehouseId = order.WarehouseId;

            // =========================================================================
            // فحص الأرصدة: استثناء المنتجات الوسيطة والعجين الداخلي
            // =========================================================================
            var allComponentIds = request.ExecutionDetails
                .SelectMany(d => d.Materials.Select(m => m.ComponentItemId))
                .Distinct()
                .ToList();

            var itemsMetadata = await _context.Items
                .AsNoTracking()
                .Where(i => allComponentIds.Contains(i.Id))
                .ToDictionaryAsync(i => i.Id, i => new { i.ItemType, i.ItemName });

            var insufficientItems = new List<string>();

            foreach (var detail in request.ExecutionDetails)
            {
                foreach (var material in detail.Materials)
                {
                    itemsMetadata.TryGetValue(material.ComponentItemId, out var meta);
                    var type = meta?.ItemType?.Trim() ?? "";
                    var name = meta?.ItemName?.Trim() ?? material.ComponentName;

                    if (type == "منتج وسيط" || type.Contains("وسيط") || name.Contains("عجين") || name.Contains("خلطة"))
                    {
                        continue;
                    }

                    var currentBalance = await _context.StockBalances
                        .Where(b => b.CompanyId == companyId && b.WarehouseId == warehouseId && b.ItemId == material.ComponentItemId)
                        .Select(b => b.AvailableQuantity)
                        .FirstOrDefaultAsync();

                    if (currentBalance < material.ActualConsumedQuantity)
                    {
                        insufficientItems.Add($"- {material.ComponentName}: (المطلوب: {material.ActualConsumedQuantity} | المتاح بالرصيد: {currentBalance})");
                    }
                }
            }

            if (insufficientItems.Any())
            {
                return BadRequest($"عفواً، لا يمكن تقفيل الأمر لعجز في رصيد الخامات:\n{string.Join("\n", insufficientItems)}");
            }

            // =========================================================================
            // تنفيذ حركة التقفيل وترصيد المنتج التام وخصم الخامات بحركة ذرية
            // =========================================================================
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                foreach (var detail in request.ExecutionDetails)
                {
                    var dbDetail = await _context.ProductionOrderDetails.FindAsync(detail.OrderDetailId);
                    if (dbDetail != null)
                    {
                        dbDetail.ProducedQuantity = detail.ProducedQuantity;
                        dbDetail.Status = "مكتمل";

                        // حساب إجمالي تكلفة الخامات الفعلية المستهلكة في الوجبة
                        decimal totalMaterialsCost = 0;

                        // صرف الخامات والمكونات المستهلكة
                        foreach (var material in detail.Materials)
                        {
                            decimal lineTotalCost = material.ActualConsumedQuantity * material.UnitCost;
                            totalMaterialsCost += lineTotalCost;

                            var rawMaterialMovement = new StockMovement
                            {
                                CompanyId = companyId,
                                WarehouseId = warehouseId,
                                ItemId = material.ComponentItemId,
                                Quantity = material.ActualConsumedQuantity,
                                UnitCost = material.UnitCost,
                                MovementType = "PRODUCTION_OUT",
                                ReferenceType = "PRODUCTION_ORDER",
                                ReferenceId = order.Id,
                                MovementAt = DateTime.UtcNow,
                                Notes = $"صرف استهلاك لأمر تشغيل {order.OrderNumber} - منتج: {detail.ProductName}"
                            };
                            _context.StockMovements.Add(rawMaterialMovement);

                            // خصم الكمية من الرصيد الكلي والمتاح
                            await UpdateStockBalance(companyId, warehouseId, material.ComponentItemId, -material.ActualConsumedQuantity);
                        }

                        // حساب تكلفة الوحدة التامة (Batch Costing)
                        decimal finishedGoodUnitCost = detail.ProducedQuantity > 0
                            ? Math.Round(totalMaterialsCost / detail.ProducedQuantity, 4)
                            : 0;

                        string batchNumber = !string.IsNullOrWhiteSpace(detail.BatchNumber)
                            ? detail.BatchNumber
                            : $"LOT-PRD-{DateTime.UtcNow:yyyyMMdd}-{order.OrderNumber}";

                        DateTime expiryDate = detail.ExpiryDate ?? DateTime.UtcNow.Date.AddDays(30);

                        // إيداع المنتج التام بالصيغة المتوافقة مع كارت الصنف وتتبع الصلاحيات
                        var finishedGoodMovement = new StockMovement
                        {
                            CompanyId = companyId,
                            WarehouseId = warehouseId,
                            ItemId = detail.ProductItemId,
                            Quantity = detail.ProducedQuantity,
                            UnitCost = finishedGoodUnitCost,
                            MovementType = "PRODUCTION_IN",
                            ReferenceType = "PRODUCTION_ORDER",
                            ReferenceId = order.Id,
                            MovementAt = DateTime.UtcNow,
                            Notes = $"استلام إنتاج تام من أمر تشغيل {order.OrderNumber} | تشغيلة: {batchNumber} | صلاحية: {expiryDate:yyyy-MM-dd} | تكلفة: {finishedGoodUnitCost:N2} ج.م"
                        };
                        _context.StockMovements.Add(finishedGoodMovement);

                        // إضافة المنتج التام للرصيد
                        await UpdateStockBalance(companyId, warehouseId, detail.ProductItemId, detail.ProducedQuantity);
                    }
                }

                order.Status = "مكتمل";
                order.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Ok(new { Message = "تم تقفيل أمر التشغيل بنجاح، وترصيد المنتج التام برقم التشغيلة والصلاحية، وخصم الخامات وحساب التكلفة." });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, $"خطأ أثناء تقفيل الإنتاج: {ex.InnerException?.Message ?? ex.Message}");
            }
        }

        private async Task UpdateStockBalance(long companyId, long warehouseId, long itemId, decimal quantityChange)
        {
            var balance = await _context.StockBalances
                .FirstOrDefaultAsync(b => b.CompanyId == companyId && b.WarehouseId == warehouseId && b.ItemId == itemId);

            if (balance != null)
            {
                balance.Quantity += quantityChange;
                balance.AvailableQuantity += quantityChange;
                balance.LastUpdatedAt = DateTime.UtcNow;
            }
            else
            {
                _context.StockBalances.Add(new StockBalance
                {
                    CompanyId = companyId,
                    WarehouseId = warehouseId,
                    ItemId = itemId,
                    Quantity = quantityChange,
                    AvailableQuantity = quantityChange,
                    LastUpdatedAt = DateTime.UtcNow
                });
            }
        }
    }

    public class PendingOrderDto
    {
        public long OrderId { get; set; }
        public string OrderNumber { get; set; } = string.Empty;
        public DateOnly OrderDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? Notes { get; set; }
    }

    public class OrderExecutionDetailDto
    {
        public long OrderDetailId { get; set; }
        public long ProductItemId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public long? RecipeId { get; set; }
        public decimal PlannedQuantity { get; set; }
        public decimal ProducedQuantity { get; set; }
        public string BatchNumber { get; set; } = string.Empty;
        public DateTime? ExpiryDate { get; set; }
        public List<ExecutionMaterialDto> Materials { get; set; } = new();
    }

    public class ExecutionMaterialDto
    {
        public long ComponentItemId { get; set; }
        public string ComponentName { get; set; } = string.Empty;
        public decimal RequiredQuantity { get; set; }
        public decimal ActualConsumedQuantity { get; set; }
        public string Unit { get; set; } = string.Empty;
        public decimal UnitCost { get; set; }
    }

    public class ExecuteOrderRequestDto
    {
        public long OrderId { get; set; }
        public List<OrderExecutionDetailDto> ExecutionDetails { get; set; } = new();
    }
}