using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AbuAdelERP.Api.Models;

[Table("accounts")]
public class Account
{
    [Key]
    [Column("id")]
    public long Id { get; set; }

    [Column("company_id")]
    public long CompanyId { get; set; }

    [Required]
    [Column("account_code")]
    [MaxLength(50)]
    public string AccountCode { get; set; } = string.Empty;

    [Required]
    [Column("account_name")]
    [MaxLength(200)]
    public string AccountName { get; set; } = string.Empty;

    [Required]
    [Column("account_type")]
    [MaxLength(50)]
    public string AccountType { get; set; } = string.Empty; // مثل: ASSET, LIABILITY, EXPENSE, REVENUE

    [Column("parent_account_id")]
    public long? ParentAccountId { get; set; } // لعمل شجرة متفرعة (حساب رئيسي وحساب فرعي)

    [Column("active")]
    public bool Active { get; set; } = true;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}