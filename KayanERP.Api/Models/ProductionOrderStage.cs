using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace KayanERP.Api.Models;

public partial class ProductionOrderStage
{
    public long Id { get; set; }

    // تم التعديل هنا لترتبط المرحلة بالصنف مباشرة
    public long ProductionOrderDetailId { get; set; }

    public int StageNumber { get; set; }
    public long ItemId { get; set; }
    public long? RecipeStageId { get; set; }

    public decimal PlannedQuantity { get; set; }
    public decimal ProducedQuantity { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public virtual Item? Item { get; set; }
    public virtual ProductionOrderDetail? ProductionOrderDetail { get; set; }
    public virtual RecipeStage? RecipeStage { get; set; }

    public virtual ICollection<ProductionStageMaterial> ProductionStageMaterials { get; set; } = new List<ProductionStageMaterial>();
}