using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using KayanERP.Api.Models.Interfaces;

namespace KayanERP.Api.Models;

[Table("branches")]
public partial class Branch : ITenantEntity
{
    [Column("id")]
    public long Id { get; set; }

    [Column("company_id")]
    public long CompanyId { get; set; } = 1;

    [Column("branch_code")]
    public string BranchCode { get; set; } = string.Empty;

    [Column("branch_name")]
    public string BranchName { get; set; } = string.Empty;

    [Column("address")]
    public string? Address { get; set; }

    [Column("phone")]
    public string? Phone { get; set; }

    [Column("active")]
    public bool Active { get; set; } = true;

    [Column("created_at")]
    public DateTime? CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime? UpdatedAt { get; set; }

    // ===== Navigation Properties =====
    public virtual Company Company { get; set; } = null!;
    public virtual ICollection<Warehouse> Warehouses { get; set; } = new List<Warehouse>();
    public virtual ICollection<SalesOrder> SalesOrders { get; set; } = new List<SalesOrder>();

    // ===== Computed Properties =====
    [NotMapped]
    public string Code => BranchCode;

    [NotMapped]
    public string NameAr => BranchName;

    [NotMapped]
    public bool IsActive => Active;
}