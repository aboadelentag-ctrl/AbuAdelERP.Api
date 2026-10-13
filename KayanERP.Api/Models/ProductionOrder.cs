using System;
using System.Collections.Generic;

namespace KayanERP.Api.Models;

public partial class ProductionOrder
{
    public long Id { get; set; }
    public long CompanyId { get; set; }
    public long WarehouseId { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public DateOnly OrderDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public long? CreatedBy { get; set; }

    public virtual Company? Company { get; set; }
    public virtual Warehouse? Warehouse { get; set; }

    // العلاقة الجديدة: أمر التشغيل الواحد يحتوي على عدة أصناف (تفاصيل)
    public virtual ICollection<ProductionOrderDetail> ProductionOrderDetails { get; set; } = new List<ProductionOrderDetail>();
}