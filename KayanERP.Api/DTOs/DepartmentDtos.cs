namespace KayanERP.Api.DTOs;

public class DepartmentDto
{
    public long Id { get; set; }
    public long CompanyId { get; set; }
    public bool Active { get; set; } = true;
    public string DepartmentName { get; set; } = string.Empty;
    public string DepartmentCode { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public long? ParentDepartmentId { get; set; }
}