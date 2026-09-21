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

        public async Task<List<RecipeDto>> GetAllRecipesAsync()
        {
            var recipes = await _context.Recipes
                .AsNoTracking()
                .Include(r => r.ProductItem)
                .Include(r => r.RecipeLines)
                .OrderByDescending(r => r.Id)
                .ToListAsync();

            var latestCosts = new Dictionary<long, decimal>();
            try
            {
                var receipts = await _context.StockMovements
                    .AsNoTracking()
                    .Where(m => m.MovementType == "PURCHASE_RECEIPT")
                    .OrderByDescending(m => m.Id)
                    .Select(m => new { m.ItemId, m.UnitCost })
                    .Take(500)
                    .ToListAsync();

                latestCosts = receipts
                    .GroupBy(m => m.ItemId)
                    .ToDictionary(g => g.Key, g => g.First().UnitCost);
            }
            catch { }

            return recipes.Select(r =>
            {
                decimal totalCost = 0;
                foreach (var line in r.RecipeLines)
                {
                    decimal cost = latestCosts.ContainsKey(line.ComponentItemId) ? latestCosts[line.ComponentItemId] : 0;
                    totalCost += (line.Quantity * (1 + (line.WastePercentage / 100))) * cost;
                }

                return new RecipeDto
                {
                    Id = r.Id,
                    CompanyId = r.CompanyId,
                    ProductItemId = r.ProductItemId,
                    ProductItemCode = r.ProductItem?.ItemCode ?? "",
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
                    LinesCount = r.RecipeLines.Count,
                    TotalEstimatedCost = totalCost
                };
            }).ToList();
        }

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

            var latestCosts = new Dictionary<long, decimal>();
            try
            {
                var receipts = await _context.StockMovements
                    .AsNoTracking()
                    .Where(m => m.MovementType == "PURCHASE_RECEIPT" && compIds.Contains(m.ItemId))
                    .OrderByDescending(m => m.Id)
                    .Select(m => new { m.ItemId, m.UnitCost })
                    .Take(500)
                    .ToListAsync();

                latestCosts = receipts
                    .GroupBy(m => m.ItemId)
                    .ToDictionary(g => g.Key, g => g.First().UnitCost);
            }
            catch { }

            var linesDto = r.RecipeLines.Select(l =>
            {
                decimal unitCost = latestCosts.ContainsKey(l.ComponentItemId) ? latestCosts[l.ComponentItemId] : 0;
                return new RecipeLineDto
                {
                    Id = l.Id,
                    ComponentItemId = l.ComponentItemId,
                    ComponentItemCode = itemsMap.ContainsKey(l.ComponentItemId) ? itemsMap[l.ComponentItemId].ItemCode : "",
                    ComponentItemName = itemsMap.ContainsKey(l.ComponentItemId) ? itemsMap[l.ComponentItemId].ItemName : "",
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
                ProductItemCode = r.ProductItem?.ItemCode ?? "",
                ProductItemName = r.ProductItem?.ItemName ?? "",
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

        public async Task<bool> SaveRecipeAsync(SaveRecipeDto dto)
        {
            if (dto == null || dto.ProductItemId <= 0 || dto.Lines == null || dto.Lines.Count == 0)
                return false;

            DateTime now = DateTime.UtcNow;

            if (dto.Id == 0)
            {
                var recipe = new Recipe
                {
                    CompanyId = dto.CompanyId > 0 ? dto.CompanyId : 1,
                    ProductItemId = dto.ProductItemId,
                    RecipeCode = string.IsNullOrWhiteSpace(dto.RecipeCode) ? $"BOM-{DateTime.UtcNow:yyyyMMdd}-{dto.ProductItemId}" : dto.RecipeCode,
                    RecipeName = dto.RecipeName,
                    BatchQuantity = dto.BatchQuantity,
                    BatchUnit = dto.BatchUnit,
                    YieldQuantity = dto.YieldQuantity,
                    YieldUnit = dto.YieldUnit,
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
                    _context.RecipeLines.Add(new RecipeLine
                    {
                        RecipeId = recipe.Id,
                        ComponentItemId = l.ComponentItemId,
                        LineNumber = lineNum++,
                        Quantity = l.Quantity,
                        Unit = l.Unit,
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
                var recipe = await _context.Recipes
                    .Include(r => r.RecipeLines)
                    .FirstOrDefaultAsync(r => r.Id == dto.Id);

                if (recipe == null) return false;

                recipe.ProductItemId = dto.ProductItemId;
                recipe.RecipeCode = dto.RecipeCode;
                recipe.RecipeName = dto.RecipeName;
                recipe.BatchQuantity = dto.BatchQuantity;
                recipe.BatchUnit = dto.BatchUnit;
                recipe.YieldQuantity = dto.YieldQuantity;
                recipe.YieldUnit = dto.YieldUnit;
                recipe.PiecesPerBatch = dto.PiecesPerBatch;
                recipe.WastePercentage = dto.WastePercentage;
                recipe.PreparationTimeMinutes = dto.PreparationTimeMinutes;
                recipe.Active = dto.Active;
                recipe.Notes = dto.Notes;
                recipe.UpdatedAt = now;

                _context.RecipeLines.RemoveRange(recipe.RecipeLines);

                int lineNum = 1;
                foreach (var l in dto.Lines)
                {
                    _context.RecipeLines.Add(new RecipeLine
                    {
                        RecipeId = recipe.Id,
                        ComponentItemId = l.ComponentItemId,
                        LineNumber = lineNum++,
                        Quantity = l.Quantity,
                        Unit = l.Unit,
                        WastePercentage = l.WastePercentage,
                        IsOptional = false,
                        Notes = l.Notes,
                        CreatedAt = now,
                        UpdatedAt = now
                    });
                }
            }

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteRecipeAsync(long id)
        {
            var recipe = await _context.Recipes
                .Include(r => r.RecipeLines)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (recipe == null) return false;

            _context.RecipeLines.RemoveRange(recipe.RecipeLines);
            _context.Recipes.Remove(recipe);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}