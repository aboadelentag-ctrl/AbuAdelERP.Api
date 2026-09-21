using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AbuAdelERP.Api.Models;

[Table("employees", Schema = "erp")]
public class Employee
{
    [Key]
    [Column("id")]
    public long Id { get; set; }

    [Column("company_id")]
    public long CompanyId { get; set; }

    [Column("branch_id")]
    public long? BranchId { get; set; } // مجهز للفروع المستقبلية

    [Column("department_id")]
    public long? DepartmentId { get; set; }

    [Column("job_title_id")]
    public long? JobTitleId { get; set; } // الربط بالمسمى الوظيفي

    // الحقل السحري لرسم شجرة المديرين (Self-Referencing)
    [Column("manager_id")]
    public long? ManagerId { get; set; }

    [Required]
    [Column("employee_name")]
    [MaxLength(150)]
    public string Name { get; set; } = string.Empty;

    [Column("employee_code")]
    [MaxLength(50)]
    public string EmployeeCode { get; set; } = string.Empty;

    [Column("active")]
    public bool Active { get; set; } = true;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // --- العلاقات المرجعية ---
    [ForeignKey("CompanyId")]
    public virtual Company? Company { get; set; }

    [ForeignKey("BranchId")]
    public virtual Branch? Branch { get; set; }

    [ForeignKey("DepartmentId")]
    public virtual Department? Department { get; set; }

    [ForeignKey("JobTitleId")]
    public virtual JobTitle? JobTitle { get; set; }

    [ForeignKey("ManagerId")]
    public virtual Employee? Manager { get; set; }
}