namespace KayanERP.UI.Models
{
    public class RecipeLineDto
    {
        public long Id { get; set; }
        public long ComponentItemId { get; set; }
        public string ComponentItemCode { get; set; } = string.Empty;
        public string ComponentItemName { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public string Unit { get; set; } = string.Empty;
        public decimal WastePercentage { get; set; } = 0;
        public decimal UnitCost { get; set; } = 0;
        public decimal LineTotalCost => (Quantity * (1 + (WastePercentage / 100))) * UnitCost;
        public string? Notes { get; set; }
    }

    public class RecipeDto
    {
        public long Id { get; set; }
        public long CompanyId { get; set; }
        public long ProductItemId { get; set; }
        public string ProductItemCode { get; set; } = string.Empty;
        public string ProductItemName { get; set; } = string.Empty;
        public string RecipeCode { get; set; } = string.Empty;
        public string RecipeName { get; set; } = string.Empty;
        public decimal BatchQuantity { get; set; } = 1;
        public string BatchUnit { get; set; } = "صاج";
        public decimal YieldQuantity { get; set; } = 1;
        public string YieldUnit { get; set; } = "صاج";
        public decimal? PiecesPerBatch { get; set; }
        public decimal WastePercentage { get; set; } = 0;
        public int? PreparationTimeMinutes { get; set; }
        public bool Active { get; set; } = true;
        public string? Notes { get; set; }
        public int LinesCount { get; set; }
        public decimal TotalEstimatedCost { get; set; }
        public decimal CostPerPiece => (PiecesPerBatch.HasValue && PiecesPerBatch.Value > 0) ? (TotalEstimatedCost / PiecesPerBatch.Value) : 0;
        public List<RecipeLineDto> Lines { get; set; } = new();
    }

    public class SaveRecipeDto
    {
        public long Id { get; set; }
        public long CompanyId { get; set; } = 1;
        public long ProductItemId { get; set; }
        public string RecipeCode { get; set; } = string.Empty;
        public string RecipeName { get; set; } = string.Empty;
        public decimal BatchQuantity { get; set; } = 1;
        public string BatchUnit { get; set; } = "صاج";
        public decimal YieldQuantity { get; set; } = 1;
        public string YieldUnit { get; set; } = "صاج";
        public decimal? PiecesPerBatch { get; set; }
        public decimal WastePercentage { get; set; } = 0;
        public int? PreparationTimeMinutes { get; set; }
        public bool Active { get; set; } = true;
        public string? Notes { get; set; }
        public List<RecipeLineDto> Lines { get; set; } = new();
    }
}