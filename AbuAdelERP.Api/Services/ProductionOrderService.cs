using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using AbuAdelERP.Api.Models;
using AbuAdelERP.Api.DTOs;

namespace AbuAdelERP.Api.Services
{
    public class ProductionOrderService : IProductionOrderService
    {
        private readonly AppDbContext _context;

        public ProductionOrderService(AppDbContext context)
        {
            _context = context;
        }

        // ==============================================================
        // 1. استعراض كافة أوامر الإنتاج (سريع ومفصول عن التتبع)
        // ==============================================================
        public async Task<List<ProductionOrderListDto>> GetAllOrdersAsync()
        {
            return await _context.ProductionOrders
                .AsNoTracking()
                .Include(p => p.Warehouse)
                .Include(p => p.ProductionOrderDetails)
                    .ThenInclude(d => d.Item)
                .Include(p => p.ProductionOrderDetails)
                    .ThenInclude(d => d.Recipe)
                .OrderByDescending(p => p.Id)
                .Select(o => new ProductionOrderListDto
                {
                    Id = o.Id,
                    OrderNumber = o.OrderNumber,
                    WarehouseName = o.Warehouse != null ? o.Warehouse.WarehouseName : "المخزن الرئيسي",
                    OrderDate = o.OrderDate.ToString("yyyy-MM-dd"),
                    Status = o.Status,
                    ItemsCount = o.ProductionOrderDetails.Count,
                    Notes = o.Notes,
                    Details = o.ProductionOrderDetails.Select(d => new ProductionOrderDetailViewDto
                    {
                        Id = d.Id,
                        ItemId = d.ItemId,
                        ItemCode = d.Item != null ? d.Item.ItemCode : string.Empty,
                        ItemName = d.Item != null ? d.Item.ItemName : string.Empty,
                        RecipeName = d.Recipe != null ? d.Recipe.RecipeName : string.Empty,
                        PlannedQuantity = d.PlannedQuantity,
                        ProducedQuantity = d.ProducedQuantity,
                        Unit = d.Unit,
                        BatchNumber = d.BatchNumber ?? string.Empty,
                        Status = d.Status
                    }).ToList()
                })
                .ToListAsync();
        }

        // ==============================================================
        // 2. جلب تفاصيل أمر إنتاج محدد بشجرة مواده وخامات مراحله
        // ==============================================================
        public async Task<ProductionOrderListDto?> GetOrderByIdAsync(long id)
        {
            var o = await _context.ProductionOrders
                .AsNoTracking()
                .Include(p => p.Warehouse)
                .Include(p => p.ProductionOrderDetails)
                    .ThenInclude(d => d.Item)
                .Include(p => p.ProductionOrderDetails)
                    .ThenInclude(d => d.Recipe)
                .Include(p => p.ProductionOrderDetails)
                    .ThenInclude(d => d.ProductionOrderStages)
                        .ThenInclude(s => s.ProductionStageMaterials)
                            .ThenInclude(m => m.Item)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (o == null) return null;

            return new ProductionOrderListDto
            {
                Id = o.Id,
                OrderNumber = o.OrderNumber,
                WarehouseName = o.Warehouse != null ? o.Warehouse.WarehouseName : string.Empty,
                OrderDate = o.OrderDate.ToString("yyyy-MM-dd"),
                Status = o.Status,
                ItemsCount = o.ProductionOrderDetails.Count,
                Notes = o.Notes,
                Details = o.ProductionOrderDetails.Select(d => new ProductionOrderDetailViewDto
                {
                    Id = d.Id,
                    ItemId = d.ItemId,
                    ItemCode = d.Item != null ? d.Item.ItemCode : string.Empty,
                    ItemName = d.Item != null ? d.Item.ItemName : string.Empty,
                    RecipeName = d.Recipe != null ? d.Recipe.RecipeName : string.Empty,
                    PlannedQuantity = d.PlannedQuantity,
                    ProducedQuantity = d.ProducedQuantity,
                    Unit = d.Unit,
                    BatchNumber = d.BatchNumber ?? string.Empty,
                    Status = d.Status,
                    Materials = d.ProductionOrderStages
                        .SelectMany(s => s.ProductionStageMaterials)
                        .Select(m => new ProductionOrderMaterialViewDto
                        {
                            ItemId = m.ItemId,
                            ItemCode = m.Item != null ? m.Item.ItemCode : string.Empty,
                            ItemName = m.Item != null ? m.Item.ItemName : string.Empty,
                            RequiredQuantity = m.RequiredQuantity,
                            ConsumedQuantity = m.ConsumedQuantity,
                            Unit = m.Unit
                        }).ToList()
                }).ToList()
            };
        }

        // ==============================================================
        // 3. إصدار أمر التشغيل وتفجير معايير الخامات (BOM Explosion) ذرياً
        // ==============================================================
        public async Task<bool> CreateOrderAsync(CreateProductionOrderDto dto)
        {
            if (dto == null || dto.Items == null || dto.Items.Count == 0)
                return false;

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                DateTime now = DateTime.UtcNow;
                string generatedOrderNo = string.IsNullOrWhiteSpace(dto.OrderNumber)
                    ? $"PRD-{DateTime.UtcNow:yyyyMMdd}-{new Random().Next(100, 999)}"
                    : dto.OrderNumber.Trim();

                // 1. إنشاء رأس أمر التشغيل
                var order = new ProductionOrder
                {
                    CompanyId = dto.CompanyId > 0 ? dto.CompanyId : 1,
                    WarehouseId = dto.WarehouseId,
                    OrderNumber = generatedOrderNo,
                    OrderDate = dto.OrderDate,
                    Status = "Pending_Execution",
                    Notes = dto.Notes,
                    CreatedAt = now,
                    UpdatedAt = now
                };

                _context.ProductionOrders.Add(order);
                await _context.SaveChangesAsync();

                // 2. تحميل كافة الوصفات المطلوبة مسبقاً دفعة واحدة لتفادي استعلامات N+1
                var recipeIds = dto.Items.Select(i => i.RecipeId).Distinct().ToList();
                var recipesMap = await _context.Recipes
                    .AsNoTracking()
                    .Include(r => r.RecipeLines)
                    .Where(r => recipeIds.Contains(r.Id))
                    .ToDictionaryAsync(r => r.Id, r => r);

                int itemCounter = 1;

                // 3. معالجة تفاصيل الأصناف ومراحل التشغيل
                foreach (var itemInput in dto.Items)
                {
                    if (!recipesMap.TryGetValue(itemInput.RecipeId, out var recipe))
                        continue;

                    string lotNumber = $"LOT-{dto.OrderDate:yyyyMMdd}-{itemInput.ItemId}-{itemCounter++}";

                    var detail = new ProductionOrderDetail
                    {
                        ProductionOrderId = order.Id,
                        ItemId = itemInput.ItemId,
                        RecipeId = itemInput.RecipeId,
                        PlannedQuantity = itemInput.PlannedQuantity,
                        ProducedQuantity = 0,
                        Unit = itemInput.Unit,
                        BatchNumber = lotNumber,
                        Status = "Planned",
                        CreatedAt = now,
                        UpdatedAt = now
                    };

                    _context.ProductionOrderDetails.Add(detail);
                    await _context.SaveChangesAsync();

                    var stage = new ProductionOrderStage
                    {
                        ProductionOrderDetailId = detail.Id,
                        StageNumber = 1,
                        ItemId = itemInput.ItemId,
                        PlannedQuantity = itemInput.PlannedQuantity,
                        ProducedQuantity = 0,
                        Status = "Ready",
                        CreatedAt = now,
                        UpdatedAt = now
                    };

                    _context.ProductionOrderStages.Add(stage);
                    await _context.SaveChangesAsync();

                    decimal batchMultiplier = recipe.BatchQuantity > 0
                        ? (itemInput.PlannedQuantity / recipe.BatchQuantity)
                        : itemInput.PlannedQuantity;

                    foreach (var line in recipe.RecipeLines)
                    {
                        decimal requiredQty = (line.Quantity * batchMultiplier) * (1m + (line.WastePercentage / 100m));

                        var stageMaterial = new ProductionStageMaterial
                        {
                            ProductionStageId = stage.Id,
                            ItemId = line.ComponentItemId,
                            RequiredQuantity = Math.Round(requiredQty, 3),
                            ConsumedQuantity = 0,
                            Unit = line.Unit,
                            WastePercentage = line.WastePercentage,
                            RecipeLineId = line.Id,
                            CreatedAt = now
                        };

                        _context.ProductionStageMaterials.Add(stageMaterial);
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
    }
}