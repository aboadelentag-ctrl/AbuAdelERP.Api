using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using KayanERP.Api.Models.Interfaces;

namespace KayanERP.Api.Models;

/// <summary>
/// عميل عهدة الأسبات (Crate Client) — يحمل رصيد الأسبات المستلمة/المسلّمة.
/// </summary>
[Table("crate_clients", Schema = "erp")]
public partial class CrateClient : ITenantEntity
{
    [Key]
    [Column("id")]
    public long Id { get; set; }

    [Column("company_id")]
    public long CompanyId { get; set; }

    [Required]
    [Column("name")]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// رصيد الأسبات الحالي مع العميل (موجب = معه أسبات، سالب = له أسبات).
    /// </summary>
    [Column("crate_balance")]
    public int CrateBalance { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // ===== Navigation Properties =====

    [ForeignKey(nameof(CompanyId))]
    public virtual Company? Company { get; set; }

    public virtual ICollection<CrateClientTransaction> Transactions { get; set; }
        = new List<CrateClientTransaction>();
}