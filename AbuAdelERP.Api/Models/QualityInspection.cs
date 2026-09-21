using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace AbuAdelERP.Api.Models;

public partial class QualityInspection
{
    public long Id { get; set; }

    public long CompanyId { get; set; }

    public long ItemId { get; set; }

    public long? WarehouseId { get; set; }

    public long? ProductionOrderId { get; set; }

    public long? ProductionStageId { get; set; }

    public string InspectionType { get; set; } = null!;

    public DateTime InspectionDate { get; set; }

    public decimal Quantity { get; set; }

    public string Status { get; set; } = null!;

    public string? Result { get; set; }

    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; }

    // --- معايير الجودة وسلامة الغذاء الجديدة ---
    public bool IsGmpPassed { get; set; }

    public bool IsHaccpPassed { get; set; }

    public bool IsWeightsPassed { get; set; }

    [Column(TypeName = "numeric(5,2)")]
    public decimal QualityScore { get; set; }

    public string? InspectorName { get; set; }
    // ------------------------------------

    public virtual Company Company { get; set; } = null!;

    public virtual Item Item { get; set; } = null!;

    public virtual ProductionOrder? ProductionOrder { get; set; }

    public virtual ProductionOrderStage? ProductionStage { get; set; }

    public virtual Warehouse? Warehouse { get; set; }
}