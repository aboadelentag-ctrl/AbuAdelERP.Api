using System;
using System.Collections.Generic;

namespace KayanERP.Api.Models;

public partial class ProductionOrderDetail
{
    public long Id { get; set; }
    public long ProductionOrderId { get; set; }
    public long ItemId { get; set; }
    public long? RecipeId { get; set; }
    public decimal PlannedQuantity { get; set; }
    public decimal ProducedQuantity { get; set; }
    public string Unit { get; set; } = string.Empty;
    public string? BatchNumber { get; set; }
    public string Status { get; set; } = "Planned";
    public string? Notes { get; set; }

    // الحقول الأساسية للإنتاجية والتكاليف كأرقام صريحة (Non-nullable)
    public long? EmployeeId { get; set; }
    public decimal WorkingHours { get; set; } = 0;
    public decimal WasteQuantity { get; set; } = 0;

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public virtual ProductionOrder? ProductionOrder { get; set; }
    public virtual Item? Item { get; set; }
    public virtual Recipe? Recipe { get; set; }
    public virtual Employee? Employee { get; set; }
    public virtual ICollection<ProductionOrderStage> ProductionOrderStages { get; set; } = new List<ProductionOrderStage>();
}