using System.ComponentModel.DataAnnotations;

namespace KayanERP.Api.DTOs;

public class ProductionOrderCreateDto
{
    [Required]
    public long ProductItemId { get; set; } // رقم المنتج التام المطلوب تصنيعه

    [Required]
    public decimal PlannedQuantity { get; set; } // الكمية المراد إنتاجها

    [Required]
    public long RawMaterialWarehouseId { get; set; } // المخزن الذي سنسحب منه الخامات

    [Required]
    public long FinishedGoodsWarehouseId { get; set; } // المخزن الذي سنضع فيه المنتج التام

    public string Notes { get; set; } = "أمر إنتاج فوري";

    public long? EmployeeId { get; set; } // رقم العامل

    public decimal WorkingHours { get; set; } // عدد الساعات المستغرقة

    public decimal WasteQuantity { get; set; } // كمية الهالك من الخامات
}