using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AbuAdelERP.Api.Models
{
    [Table("users", Schema = "erp")]
    public class AppUser
    {
        [Key]
        [Column("id")]
        public long Id { get; set; }

        [Column("company_id")]
        public long? CompanyId { get; set; } = 1;

        [Column("role_id")]
        public long? RoleId { get; set; } = 1;

        [Column("employee_id")]
        public long? EmployeeId { get; set; }

        [Column("employee_code")]
        public string? EmployeeCode { get; set; }

        [Required]
        [Column("username")]
        public string Username { get; set; } = string.Empty;

        [Required]
        [Column("password_hash")]
        public string PasswordHash { get; set; } = string.Empty;

        [Required]
        [Column("role")]
        public string Role { get; set; } = "User";

        [Column("full_name")]
        public string? FullName { get; set; }

        [Column("department_id")]
        public long? DepartmentId { get; set; }

        [Column("department_name")]
        public string? DepartmentName { get; set; }

        [Column("is_active")]
        public bool IsActive { get; set; } = true;

        [NotMapped]
        public bool Active => IsActive;

        [Column("custom_permissions")]
        public string? CustomPermissions { get; set; }

        [ForeignKey(nameof(CompanyId))]
        public virtual Company? Company { get; set; }
    }

    [Table("roles", Schema = "erp")]
    public class AppRole
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Required]
        [Column("role_code")]
        public string Name { get; set; } = string.Empty;

        [Required]
        [Column("role_name")]
        public string DisplayName { get; set; } = string.Empty;

        [Column("description")]
        public string? Description { get; set; }

        [Column("permissions")]
        public string Permissions { get; set; } = string.Empty;

        [Column("company_id")]
        public int? CompanyId { get; set; } = 1;

        [Column("active")]
        public bool Active { get; set; } = true;

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}