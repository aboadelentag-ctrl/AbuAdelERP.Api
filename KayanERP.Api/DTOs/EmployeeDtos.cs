namespace KayanERP.Api.DTOs;

public class JobTitleLookupDto
{
    public long Id { get; set; }
    public string TitleName { get; set; } = string.Empty;
    public decimal HourlyRate { get; set; } = 25;
    public int JobGrade { get; set; } = 10;
    public bool Active { get; set; } = true;
}

public class CreateJobTitleDto
{
    public string TitleName { get; set; } = string.Empty;
    public int JobGrade { get; set; } = 10;
    public decimal HourlyRate { get; set; } = 25;
}

public class EmployeeListItemDto
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string EmployeeCode { get; set; } = string.Empty;
    public long? JobTitleId { get; set; }
    public string? JobTitle { get; set; }
    public long? DepartmentId { get; set; }
    public string? DepartmentName { get; set; }
    public bool Active { get; set; }
}

public class CreateEmployeeDto
{
    public string Name { get; set; } = string.Empty;
    public string? EmployeeCode { get; set; }
    public long? DepartmentId { get; set; }
    public long? JobTitleId { get; set; }
    public long? ManagerId { get; set; }
    public bool Active { get; set; } = true;
}

public class UpdateEmployeeDto
{
    public string Name { get; set; } = string.Empty;
    public string? EmployeeCode { get; set; }
    public long? DepartmentId { get; set; }
    public long? JobTitleId { get; set; }
    public bool Active { get; set; } = true;
}