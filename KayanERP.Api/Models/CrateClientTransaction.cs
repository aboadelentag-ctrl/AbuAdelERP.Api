using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using KayanERP.Api.Models.Interfaces;

namespace KayanERP.Api.Models;

/// <summary>
/// حركة عهدة أسبات مع عميل (صرف أو استلام).
/// </summary>
[Table("crate_client_transactions", Schema = "erp")]
public partial class CrateClientTransaction : ITenantEntity
{
    [Key]
    [Column("id")]
    public long Id { get; set; }

    [Column("company_id")]
    public long CompanyId { get; set; }

    [Column("crate_client_id")]
    public long CrateClientId { get; set; }

    /// <summary>
    /// نوع الحركة: DISPATCH (منصرف للعميل) | RETURN (مستلم من العميل).
    /// </summary>
    [Required]
    [Column("transaction_type")]
    [MaxLength(20)]
    public string TransactionType { get; set; } = string.Empty;

    [Column("quantity")]
    public int Quantity { get; set; }

    [Column("transaction_date")]
    public DateTime TransactionDate { get; set; } = DateTime.UtcNow;

    [Column("notes")]
    [MaxLength(500)]
    public string? Notes { get; set; }

    // ===== Navigation Properties =====

    [ForeignKey(nameof(CrateClientId))]
    public virtual CrateClient? CrateClient { get; set; }

    [ForeignKey(nameof(CompanyId))]
    public virtual Company? Company { get; set; }
}