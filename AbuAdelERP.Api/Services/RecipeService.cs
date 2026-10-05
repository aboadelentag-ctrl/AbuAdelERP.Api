using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using AbuAdelERP.Api.Models;
using AbuAdelERP.Api.DTOs;

namespace AbuAdelERP.Api.Services
{
    public class RecipeService : IRecipeService
    {
        private readonly AppDbContext _context;

        public RecipeService(AppDbContext context)
        {
            _context = context;
        }

        // ==============================================================
        // 1. جلب كافة معايير خلطات التصنيع مع احتساب التكاليف اللحظية
        // ==============================================================
        public async Task<List<RecipeDto>> GetAllRecipesAsync()
        {
            var recipes = await _context.Recipes
                .AsNoTracking()
                .Include(r => r.ProductItem)
                .Include(r => r.RecipeLines)
                .OrderByDescending(r => r.Id)
                .ToListAsync();

            var latestCosts = await GetLatestComponentCostsAsync();

            return recipes.Select(r =>
            {
                decimal totalCost = 0;
                foreach (var line in r.RecipeLines.Where(l => l.Quantity > 0))
                {
                    decimal cost = latestCosts.TryGetValue(line.ComponentItemId, out var unitCost) ? unitCost : 0;
                    decimal waste = line.WastePercentage;
                    totalCost += (line.Quantity * (1m + (waste / 100m))) * cost;
                }

                return new RecipeDto
                {
                    Id = r.Id,
                    CompanyId = r.CompanyId,
                    ProductItemId = r.ProductItemId,
                    ProductItemCode = r.ProductItem?.ItemCode ?? string.Empty,
                    ProductItemName = r.ProductItem?.ItemName ?? "منتج غير محدد",
                    RecipeCode = r.RecipeCode,
                    RecipeName = r.RecipeName,
                    BatchQuantity = r.BatchQuantity,
                    BatchUnit = r.BatchUnit,
                    YieldQuantity = r.YieldQuantity,
                    YieldUnit = r.YieldUnit,
                    PiecesPerBatch = r.PiecesPerBatch,
                    WastePercentage = r.WastePercentage,
                    PreparationTimeMinutes = r.PreparationTimeMinutes,
                    Active = r.Active,
                    Notes = r.Notes,
                    LinesCount = r.RecipeLines.Count(l => l.Quantity > 0),
                    TotalEstimatedCost = totalCost
                };
            }).ToList();
        }

        // ==============================================================
        // 2. جلب تفاصيل بطاقة خلطة محددة
        // ==============================================================
        public async Task<RecipeDto?> GetRecipeByIdAsync(long id)
        {
            var r = await _context.Recipes
                .AsNoTracking()
                .Include(x => x.ProductItem)
                .Include(x => x.RecipeLines)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (r == null) return null;

            var compIds = r.RecipeLines.Select(l => l.ComponentItemId).Distinct().ToList();

            var itemsMap = await _context.Items
                .AsNoTracking()
                .Where(i => compIds.Contains(i.Id))
                .ToDictionaryAsync(i => i.Id, i => new { i.ItemCode, i.ItemName });

            var latestCosts = await GetLatestComponentCostsAsync(compIds);

            var linesDto = r.RecipeLines
                .Where(l => l.Quantity > 0)
                .Select(l =>
                {
                    decimal unitCost = latestCosts.TryGetValue(l.ComponentItemId, out var cost) ? cost : 0;
                    return new RecipeLineDto
                    {
                        Id = l.Id,
                        ComponentItemId = l.ComponentItemId,
                        ComponentItemCode = itemsMap.ContainsKey(l.ComponentItemId) ? itemsMap[l.ComponentItemId].ItemCode : string.Empty,
                        ComponentItemName = itemsMap.ContainsKey(l.ComponentItemId) ? itemsMap[l.ComponentItemId].ItemName : string.Empty,
                        Quantity = l.Quantity,
                        Unit = l.Unit,
                        WastePercentage = l.WastePercentage,
                        UnitCost = unitCost,
                        Notes = l.Notes
                    };
                }).ToList();

            decimal totalCost = linesDto.Sum(l => l.LineTotalCost);

            return new RecipeDto
            {
                Id = r.Id,
                CompanyId = r.CompanyId,
                ProductItemId = r.ProductItemId,
                ProductItemCode = r.ProductItem?.ItemCode ?? string.Empty,
                ProductItemName = r.ProductItem?.ItemName ?? string.Empty,
                RecipeCode = r.RecipeCode,
                RecipeName = r.RecipeName,
                BatchQuantity = r.BatchQuantity,
                BatchUnit = r.BatchUnit,
                YieldQuantity = r.YieldQuantity,
                YieldUnit = r.YieldUnit,
                PiecesPerBatch = r.PiecesPerBatch,
                WastePercentage = r.WastePercentage,
                PreparationTimeMinutes = r.PreparationTimeMinutes,
                Active = r.Active,
                Notes = r.Notes,
                LinesCount = linesDto.Count,
                TotalEstimatedCost = totalCost,
                Lines = linesDto
            };
        }

        // ==============================================================
        // 3. حفظ واعتماد بطاقة المعايير (BOM) الفردية
        // ==============================================================
        public async Task<bool> SaveRecipeAsync(SaveRecipeDto dto)
        {
            if (dto == null || dto.ProductItemId <= 0 || dto.Lines == null || dto.Lines.Count == 0)
                return false;

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                await SaveRecipeInternalAsync(dto);
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

        // ==============================================================
        // 4. استيراد معايير وخلطات التصنيع دفعة واحدة من الإكسيل (Bulk Import)
        // ==============================================================
        public async Task<int> BulkImportRecipesAsync(List<SaveRecipeDto> dtoList)
        {
            if (dtoList == null || !dtoList.Any()) return 0;

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                int count = 0;
                foreach (var dto in dtoList)
                {
                    if (dto.ProductItemId <= 0 || dto.Lines == null || dto.Lines.Count == 0)
                        continue;

                    await SaveRecipeInternalAsync(dto);
                    count++;
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                return count;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        // ==============================================================
        // 5. حذف أو إيقاف بطاقة المعايير بأمان
        // ==============================================================
        public async Task<bool> DeleteRecipeAsync(long id)
        {
            var recipe = await _context.Recipes
                .Include(r => r.RecipeLines)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (recipe == null) return false;

            bool isUsedInProduction = await _context.ProductionOrderDetails.AnyAsync(pod => pod.RecipeId == id)
                                   || await _context.ProductionOrderStages.AnyAsync(pos => pos.RecipeStageId == id);

            if (isUsedInProduction)
            {
                recipe.Active = false;
                recipe.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                return true;
            }

            _context.RecipeLines.RemoveRange(recipe.RecipeLines);
            _context.Recipes.Remove(recipe);
            await _context.SaveChangesAsync();
            return true;
        }

        // ==============================================================
        // دوال داخلية ومساعدة (Private Helpers)
        // ==============================================================
        private async Task SaveRecipeInternalAsync(SaveRecipeDto dto)
        {
            DateTime now = DateTime.UtcNow;
            Recipe? recipe;

            if (dto.Id == 0)
            {
                recipe = new Recipe
                {
                    CompanyId = dto.CompanyId > 0 ? dto.CompanyId : 1,
                    ProductItemId = dto.ProductItemId,
                    RecipeCode = string.IsNullOrWhiteSpace(dto.RecipeCode) ? $"BOM-{DateTime.UtcNow:yyyyMMdd}-{dto.ProductItemId}" : dto.RecipeCode.Trim(),
                    RecipeName = dto.RecipeName.Trim(),
                    BatchQuantity = dto.BatchQuantity,
                    BatchUnit = !string.IsNullOrWhiteSpace(dto.BatchUnit) ? dto.BatchUnit : "كجم",
                    YieldQuantity = dto.YieldQuantity > 0 ? dto.YieldQuantity : dto.BatchQuantity,
                    YieldUnit = !string.IsNullOrWhiteSpace(dto.YieldUnit) ? dto.YieldUnit : dto.BatchUnit,
                    PiecesPerBatch = dto.PiecesPerBatch,
                    WastePercentage = dto.WastePercentage,
                    PreparationTimeMinutes = dto.PreparationTimeMinutes,
                    Active = dto.Active,
                    Notes = dto.Notes,
                    EffectiveFrom = DateOnly.FromDateTime(DateTime.Today),
                    CreatedAt = now,
                    UpdatedAt = now
                };

                _context.Recipes.Add(recipe);
                await _context.SaveChangesAsync();

                int lineNum = 1;
                foreach (var l in dto.Lines)
                {
                    if (l.ComponentItemId <= 0 || l.Quantity <= 0) continue;

                    _context.RecipeLines.Add(new RecipeLine
                    {
                        RecipeId = recipe.Id,
                        ComponentItemId = l.ComponentItemId,
                        LineNumber = lineNum++,
                        Quantity = l.Quantity,
                        Unit = !string.IsNullOrWhiteSpace(l.Unit) ? l.Unit : "كجم",
                        WastePercentage = l.WastePercentage,
                        IsOptional = false,
                        Notes = l.Notes,
                        CreatedAt = now,
                        UpdatedAt = now
                    });
                }
            }
            else
            {
                recipe = await _context.Recipes
                    .Include(r => r.RecipeLines)
                    .FirstOrDefaultAsync(r => r.Id == dto.Id);

                if (recipe == null)
                    throw new InvalidOperationException($"لم يتم العثور على الوصفة رقم {dto.Id}");

                recipe.ProductItemId = dto.ProductItemId;
                recipe.RecipeCode = !string.IsNullOrWhiteSpace(dto.RecipeCode) ? dto.RecipeCode.Trim() : recipe.RecipeCode;
                recipe.RecipeName = dto.RecipeName.Trim();
                recipe.BatchQuantity = dto.BatchQuantity;
                recipe.BatchUnit = !string.IsNullOrWhiteSpace(dto.BatchUnit) ? dto.BatchUnit : recipe.BatchUnit;
                recipe.YieldQuantity = dto.YieldQuantity > 0 ? dto.YieldQuantity : dto.BatchQuantity;
                recipe.YieldUnit = !string.IsNullOrWhiteSpace(dto.YieldUnit) ? dto.YieldUnit : recipe.BatchUnit;
                recipe.PiecesPerBatch = dto.PiecesPerBatch;
                recipe.WastePercentage = dto.WastePercentage;
                recipe.PreparationTimeMinutes = dto.PreparationTimeMinutes;
                recipe.Active = dto.Active;
                recipe.Notes = dto.Notes;
                recipe.UpdatedAt = now;

                int lineNum = 1;
                var processedLineIds = new List<long>();

                foreach (var lineDto in dto.Lines)
                {
                    if (lineDto.ComponentItemId <= 0 || lineDto.Quantity <= 0) continue;

                    var existingLine = recipe.RecipeLines.FirstOrDefault(l =>
                        (lineDto.Id > 0 && l.Id == lineDto.Id) ||
                        (l.ComponentItemId == lineDto.ComponentItemId));

                    if (existingLine != null)
                    {
                        existingLine.Quantity = lineDto.Quantity;
                        existingLine.Unit = !string.IsNullOrWhiteSpace(lineDto.Unit) ? lineDto.Unit : existingLine.Unit;
                        existingLine.WastePercentage = lineDto.WastePercentage;
                        existingLine.Notes = lineDto.Notes;
                        existingLine.LineNumber = lineNum++;
                        existingLine.UpdatedAt = now;
                        processedLineIds.Add(existingLine.Id);
                    }
                    else
                    {
                        var newLine = new RecipeLine
                        {
                            RecipeId = recipe.Id,
                            ComponentItemId = lineDto.ComponentItemId,
                            LineNumber = lineNum++,
                            Quantity = lineDto.Quantity,
                            Unit = !string.IsNullOrWhiteSpace(lineDto.Unit) ? lineDto.Unit : "كجم",
                            WastePercentage = lineDto.WastePercentage,
                            IsOptional = false,
                            Notes = lineDto.Notes,
                            CreatedAt = now,
                            UpdatedAt = now
                        };
                        _context.RecipeLines.Add(newLine);
                    }
                }

                var linesToRemove = recipe.RecipeLines.Where(l => !processedLineIds.Contains(l.Id)).ToList();
                foreach (var oldLine in linesToRemove)
                {
                    bool isReferenced = await _context.ProductionStageMaterials
                        .AnyAsync(psm => psm.RecipeLineId == oldLine.Id);

                    if (isReferenced)
                    {
                        oldLine.Quantity = 0;
                        oldLine.IsOptional = true;
                        oldLine.Notes = "[مستبعد من المعيار المحدث - تم الاحتفاظ به لتوثيق أوامر الإنتاج السابقة]";
                        oldLine.UpdatedAt = now;
                    }
                    else
                    {
                        _context.RecipeLines.Remove(oldLine);
                    }
                }
            }
        }

        private async Task<Dictionary<long, decimal>> GetLatestComponentCostsAsync(List<long>? componentIds = null)
        {
            try
            {
                var query = _context.StockMovements
                    .AsNoTracking()
                    .Where(m => m.MovementType == "PURCHASE_RECEIPT");

                if (componentIds != null && componentIds.Any())
                {
                    query = query.Where(m => componentIds.Contains(m.ItemId));
                }

                var receipts = await query
                    .OrderByDescending(m => m.Id)
                    .Select(m => new { m.ItemId, m.UnitCost })
                    .Take(500)
                    .ToListAsync();

                return receipts
                    .GroupBy(m => m.ItemId)
                    .ToDictionary(g => g.Key, g => g.First().UnitCost);
            }
            catch
            {
                return new Dictionary<long, decimal>();
            }
        }
    }
}