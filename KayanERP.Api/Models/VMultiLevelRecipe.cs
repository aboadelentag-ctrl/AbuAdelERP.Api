using System;
using System.Collections.Generic;

namespace KayanERP.Api.Models;

public partial class VMultiLevelRecipe
{
    public long? RecipeId { get; set; }

    public long? CompanyId { get; set; }

    public long? ProductItemId { get; set; }

    public string? ProductName { get; set; }

    public string? ProductType { get; set; }

    public string? RecipeCode { get; set; }

    public string? RecipeName { get; set; }

    public decimal? BatchQuantity { get; set; }

    public string? BatchUnit { get; set; }

    public decimal? YieldQuantity { get; set; }

    public string? YieldUnit { get; set; }

    public decimal? PiecesPerBatch { get; set; }

    public decimal? WastePercentage { get; set; }

    public bool? Active { get; set; }

    public DateOnly? EffectiveFrom { get; set; }

    public DateOnly? EffectiveTo { get; set; }

    public long? ComponentCount { get; set; }

    public long? IntermediateComponentCount { get; set; }

    public long? RawMaterialComponentCount { get; set; }
}
