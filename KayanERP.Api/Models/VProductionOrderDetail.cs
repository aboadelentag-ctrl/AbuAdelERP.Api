using System;
using System.Collections.Generic;

namespace KayanERP.Api.Models;

public partial class VProductionOrderDetail
{
    public long? ProductionOrderId { get; set; }
    public string? OrderNumber { get; set; }
    public DateOnly? OrderDate { get; set; }
    public long? CompanyId { get; set; }
    public long? WarehouseId { get; set; }
    public long? ProductItemId { get; set; }
    public string? ProductName { get; set; }
    public long? RecipeId { get; set; }
    public string? RecipeCode { get; set; }
    public string? RecipeName { get; set; }
    public decimal? PlannedQuantity { get; set; }
    public decimal? ProducedQuantity { get; set; }
    public decimal? PlannedPieces { get; set; }
    public decimal? ProducedPieces { get; set; }
    public string? OrderStatus { get; set; }
    public long? ProductionStageId { get; set; }
    public int? StageNumber { get; set; }
    public long? StageItemId { get; set; }
    public string? StageItemName { get; set; }
    public long? StageRecipeId { get; set; }
    public string? StageRecipeName { get; set; }
    public decimal? StagePlannedQuantity { get; set; }
    public decimal? StageProducedQuantity { get; set; }
    public string? StageStatus { get; set; }
    public long? MaterialItemId { get; set; }
    public string? MaterialName { get; set; }
    public decimal? RequiredQuantity { get; set; }
    public decimal? ConsumedQuantity { get; set; }
    public string? Unit { get; set; }
    public decimal? WastePercentage { get; set; }
}