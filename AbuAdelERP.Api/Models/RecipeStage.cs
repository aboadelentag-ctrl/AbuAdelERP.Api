using System;
using System.Collections.Generic;

namespace AbuAdelERP.Api.Models;

public partial class RecipeStage
{
    public long Id { get; set; }

    public long RecipeId { get; set; }

    public int StageNumber { get; set; }

    public string StageName { get; set; } = null!;

    public long? StageRecipeId { get; set; }

    public long? OutputItemId { get; set; }

    public string? Notes { get; set; }

    public bool Active { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual Item? OutputItem { get; set; }

    public virtual ICollection<ProductionOrderStage> ProductionOrderStages { get; set; } = new List<ProductionOrderStage>();

    public virtual Recipe Recipe { get; set; } = null!;

    public virtual ICollection<RecipeLine> RecipeLines { get; set; } = new List<RecipeLine>();

    public virtual Recipe? StageRecipe { get; set; }
}
