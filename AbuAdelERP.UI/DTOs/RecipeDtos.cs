using System;
using System.Collections.Generic;

namespace AbuAdelERP.UI.DTOs
{
    public class RecipeDto
    {
        public long Id { get; set; }
        public long CompanyId { get; set; }
        public long ProductItemId { get; set; }
        public string ProductItemCode { get; set; } = string.Empty;
        public string ProductItemName { get; set; } = string.Empty;
        public string RecipeCode { get; set; } = string.Empty;
        public string RecipeName { get; set; } = string.Empty;
        public decimal BatchQuantity { get; set; } = 10;
        public string BatchUnit { get; set; } = "كجم";
        public decimal YieldQuantity { get; set; } = 10;
        public string YieldUnit { get; set; } = "كجم";
        public decimal PiecesPerBatch { get; set; } = 0;
        public decimal WastePercentage { get; set; } = 0;
        public int PreparationTimeMinutes { get; set; } = 60;
        public bool Active { get; set; } = true;
        public string? Notes { get; set; }
        public int LinesCount { get; set; }
        public decimal TotalEstimatedCost { get; set; }
        public decimal CostPerPiece => PiecesPerBatch > 0 ? (TotalEstimatedCost / PiecesPerBatch) : 0;
        public List<RecipeLineDto> Lines { get; set; } = new();
    }

    public class SaveRecipeDto
    {
        public long Id { get; set; }
        public long CompanyId { get; set; } = 1;
        public long ProductItemId { get; set; }
        public string? RecipeCode { get; set; }
        public string RecipeName { get; set; } = string.Empty;
        public decimal BatchQuantity { get; set; } = 10;
        public string BatchUnit { get; set; } = "كجم";
        public decimal YieldQuantity { get; set; } = 10;
        public string YieldUnit { get; set; } = "كجم";
        public decimal PiecesPerBatch { get; set; } = 0;
        public decimal WastePercentage { get; set; } = 0;
        public int PreparationTimeMinutes { get; set; } = 60;
        public bool Active { get; set; } = true;
        public string? Notes { get; set; }
        public List<RecipeLineDto> Lines { get; set; } = new();
    }

    public class RecipeLineDto
    {
        public long Id { get; set; }
        public long ComponentItemId { get; set; }
        public string ComponentItemCode { get; set; } = string.Empty;
        public string ComponentItemName { get; set; } = string.Empty;
        public decimal Quantity { get; set; } = 1;
        public string Unit { get; set; } = "كجم";
        public decimal WastePercentage { get; set; } = 0;
        public decimal UnitCost { get; set; }
        public decimal LineTotalCost => (Quantity * (1m + (WastePercentage / 100m))) * UnitCost;
        public string? Notes { get; set; }
    }

    public class ItemLookupDto
    {
        public long Id { get; set; }
        public string ItemCode { get; set; } = string.Empty;
        public string ItemName { get; set; } = string.Empty;
        public string ItemType { get; set; } = string.Empty;
        public string Unit { get; set; } = string.Empty;
    }

    public class QuickItemCreateDto
    {
        public string ItemCode { get; set; } = string.Empty;
        public string ItemName { get; set; } = string.Empty;
        public string ItemType { get; set; } = "نصف مصنع";
        public string Unit { get; set; } = "كجم";
    }

    public class RecipeImportPreviewDto
    {
        public string RecipeCode { get; set; } = string.Empty;
        public string RecipeName { get; set; } = string.Empty;
        public string RawProductName { get; set; } = string.Empty;
        public long ProductItemId { get; set; }
        public string MatchedProductName { get; set; } = string.Empty;
        public decimal BatchQuantity { get; set; } = 10;
        public string BatchUnit { get; set; } = "كجم";
        public decimal YieldQuantity { get; set; } = 10;
        public decimal PiecesPerBatch { get; set; } = 0;
        public decimal WastePercentage { get; set; } = 0;
        public int PreparationTimeMinutes { get; set; } = 60;
        public List<RecipeLineDto> Lines { get; set; } = new();
    }
}