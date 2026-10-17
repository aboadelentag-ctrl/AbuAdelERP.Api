using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace KayanERP.Shared.DTOs;

public class BatchCostRequestDto
{
    [Required]
    public long ProductionOrderId { get; set; }

    [Required]
    public decimal LaborCostPerHour { get; set; } // أجر ساعة العامل التقريبي

    [Required]
    public decimal OverheadCostPerBatch { get; set; } // مصاريف التشغيل للباتشة (كهرباء، غاز، إلخ)

    // قائمة الخامات الفعلية التي تم سحبها من المخزن لهذه الباتشة
    public List<MaterialCostDto> MaterialsUsed { get; set; } = new List<MaterialCostDto>();
}

public class MaterialCostDto
{
    public string MaterialName { get; set; } = null!;
    public decimal QuantityUsed { get; set; } // الكمية المستخدمة بالكيلو أو الجرام
    public decimal UnitPrice { get; set; } // سعر الوحدة من آخر فاتورة شراء
}