using System;
using System.Collections.Generic;

namespace AbuAdelERP.Api.Models;

public partial class RecipeLine
{
    public long Id { get; set; }

    public long RecipeId { get; set; }

    public long ComponentItemId { get; set; }

    public int LineNumber { get; set; }

    public decimal Quantity { get; set; }

    public string Unit { get; set; } = null!;

    public decimal WastePercentage { get; set; }

    public bool IsOptional { get; set; }

    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public long? StageId { get; set; }

    public virtual Item? ComponentItem { get; set; }

    public virtual ICollection<ProductionStageMaterial> ProductionStageMaterials { get; set; } = new List<ProductionStageMaterial>();

    public virtual Recipe? Recipe { get; set; }

    public virtual RecipeStage? Stage { get; set; }
}
