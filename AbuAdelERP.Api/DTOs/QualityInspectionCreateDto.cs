using System.ComponentModel.DataAnnotations;

namespace AbuAdelERP.Api.DTOs;

public class QualityInspectionCreateDto
{
    [Required]
    public long ProductionOrderId { get; set; }

    [Required]
    public bool IsGmpPassed { get; set; } // هل اجتاز فحص النظافة؟

    [Required]
    public bool IsHaccpPassed { get; set; } // هل التزم بدرجات الحرارة؟

    [Required]
    public bool IsWeightsPassed { get; set; } // هل الأوزان مطابقة؟

    public string? InspectorNotes { get; set; }

    [Required]
    public string InspectorName { get; set; } = "مراقب الجودة";
}