using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AbuAdelERP.Api.Models;

[Table("departments", Schema = "erp")]
public class Department
{
    [Key]
    [Column("id")]
    public long Id { get; set; }

    [Column("company_id")]
    public long CompanyId { get; set; }

    // الحقل السحري للربط الذاتي (تفرع الإدارات)
    [Column("parent_department_id")]
    public long? ParentDepartmentId { get; set; }

    [Column("department_name")]
    public string DepartmentName { get; set; } = string.Empty;

    [Column("department_code")]
    public string DepartmentCode { get; set; } = string.Empty;

    [Column("description")]
    public string Description { get; set; } = string.Empty;

    [Column("active")]
    public bool Active { get; set; } = true;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime? UpdatedAt { get; set; }

    // --- العلاقات المرجعية ---
    [ForeignKey("CompanyId")]
    public virtual Company? Company { get; set; }

    [ForeignKey("ParentDepartmentId")]
    public virtual Department? ParentDepartment { get; set; }

    public virtual ICollection<Department> SubDepartments { get; set; } = new List<Department>();
    public virtual ICollection<MaintenanceAsset> MaintenanceAssets { get; set; } = new List<MaintenanceAsset>();
    public virtual ICollection<Employee> Employees { get; set; } = new List<Employee>();
}