namespace KayanERP.Shared.DTOs;

public class DepartmentCreateDto
{
    public string Name { get; set; } = string.Empty;
    public string DepartmentCode { get; set; } = string.Empty;
}

public class EmployeeCreateDto
{
    public long DepartmentId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string EmployeeCode { get; set; } = string.Empty;
}