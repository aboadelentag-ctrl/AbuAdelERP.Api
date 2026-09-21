using System;
using System.Collections.Generic;

namespace AbuAdelERP.Api.Models;

public partial class ProductionStageMaterial
{
    public long Id { get; set; }
    public long ProductionStageId { get; set; }
    public long ItemId { get; set; }

    public decimal RequiredQuantity { get; set; }
    public decimal ConsumedQuantity { get; set; }
    public string Unit { get; set; } = string.Empty;
    public decimal WastePercentage { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public long? RecipeLineId { get; set; }

    public virtual Item? Item { get; set; }
    public virtual ProductionOrderStage? ProductionStage { get; set; }
    public virtual RecipeLine? RecipeLine { get; set; }
}