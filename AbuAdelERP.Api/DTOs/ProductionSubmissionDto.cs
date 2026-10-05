namespace AbuAdelERP.Api.DTOs;

public class ProductionSubmissionDto
{
    public long FormId { get; set; }
    public long DepartmentId { get; set; }
    public long UserId { get; set; }
    public long CompanyId { get; set; }
    public long WarehouseId { get; set; }
    public long ProductItemId { get; set; }
    public decimal ProducedQuantity { get; set; }
    public decimal ActualHours { get; set; }
    public decimal WasteCost { get; set; }
    public string? Notes { get; set; }

    // هذا الحقل سيستقبل أي بيانات متغيرة مستقبلاً مثل درجة الحرارة أو الجودة دون الحاجة لتعديل الكود
    public Dictionary<string, object>? DynamicData { get; set; }
}

public class ProductionSubmissionResultDto
{
    public string Message { get; set; } = string.Empty;
    public long ProductionOrderId { get; set; }
}