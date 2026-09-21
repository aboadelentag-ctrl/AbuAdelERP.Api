using System;
using System.Collections.Generic;

namespace AbuAdelERP.Api.Models;

public partial class Recipe
{
    public long Id { get; set; }

    public long CompanyId { get; set; }

    public long ProductItemId { get; set; }

    public string RecipeCode { get; set; } = null!;

    public string RecipeName { get; set; } = null!;

    public decimal BatchQuantity { get; set; }

    public string BatchUnit { get; set; } = null!;

    public decimal YieldQuantity { get; set; }

    public string YieldUnit { get; set; } = null!;

    public decimal? PiecesPerBatch { get; set; }

    public decimal WastePercentage { get; set; }

    public int? PreparationTimeMinutes { get; set; }

    public bool Active { get; set; }

    public string? Notes { get; set; }

    public DateOnly EffectiveFrom { get; set; }

    public DateOnly? EffectiveTo { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual Company? Company { get; set; }
    public virtual Item? ProductItem { get; set; }

    public virtual ICollection<ProductionOrderStage> ProductionOrderStages { get; set; } = new List<ProductionOrderStage>();

    public virtual ICollection<ProductionOrder> ProductionOrders { get; set; } = new List<ProductionOrder>();

    public virtual ICollection<RecipeLine> RecipeLines { get; set; } = new List<RecipeLine>();

    public virtual ICollection<RecipeStage> RecipeStageRecipes { get; set; } = new List<RecipeStage>();

    public virtual ICollection<RecipeStage> RecipeStageStageRecipes { get; set; } = new List<RecipeStage>();
}
