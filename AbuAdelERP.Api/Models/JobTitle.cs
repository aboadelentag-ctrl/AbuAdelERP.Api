using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AbuAdelERP.Api.Models;

[Table("job_titles", Schema = "erp")]
public class JobTitle
{
    [Key]
    [Column("id")]
    public long Id { get; set; }

    [Column("company_id")]
    public long CompanyId { get; set; }

    [Required]
    [Column("title_name")]
    [MaxLength(150)]
    public string TitleName { get; set; } = string.Empty;

    [Column("job_grade")]
    public int JobGrade { get; set; } = 1; // المستوى الإداري

    // معدل تكلفة الساعة الاسترشادي لحساب تكلفة الدفعة والوجبة (دون التأثير على الراتب الشهري الثابت)
    [Column("hourly_rate", TypeName = "decimal(18,2)")]
    public decimal HourlyRate { get; set; } = 0;

    [Column("active")]
    public bool Active { get; set; } = true;

    [ForeignKey("CompanyId")]
    public virtual Company? Company { get; set; }
}