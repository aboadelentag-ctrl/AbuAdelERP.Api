using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using KayanERP.Api.Models.Interfaces;

namespace KayanERP.Api.Models;

[Table("warehouses", Schema = "erp")]
public partial class Warehouse : ITenantEntity
{
    [Column("id")]
    public long Id { get; set; }

    [Column("company_id")]
    public long CompanyId { get; set; } = 1;

    [Column("branch_id")]
    public long? BranchId { get; set; } = 1;

    [Column("warehouse_code")]
    public string WarehouseCode { get; set; } = string.Empty;

    [Column("warehouse_name")]
    public string WarehouseName { get; set; } = string.Empty;

    [Column("warehouse_type")]
    public string? WarehouseType { get; set; }

    [Column("description")]
    public string? Description { get; set; }

    [Column("parent_warehouse_id")]
    public long? ParentWarehouseId { get; set; }

    [Column("active")]
    public bool Active { get; set; } = true;

    [NotMapped]
    public bool IsActive
    {
        get => Active;
        set => Active = value;
    }

    [Column("created_at")]
    public DateTime? CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTime? UpdatedAt { get; set; }

    // ===== Navigation Properties =====
    public virtual Branch? Branch { get; set; }
    public virtual Company Company { get; set; } = null!;
    public virtual Warehouse? ParentWarehouse { get; set; }
    public virtual ICollection<Warehouse> InverseParentWarehouse { get; set; } = new List<Warehouse>();

    public virtual ICollection<ProductionOrder> ProductionOrders { get; set; } = new List<ProductionOrder>();
    public virtual ICollection<PurchaseOrder> PurchaseOrders { get; set; } = new List<PurchaseOrder>();
    public virtual ICollection<QualityInspection> QualityInspections { get; set; } = new List<QualityInspection>();
    public virtual ICollection<SalesOrder> SalesOrders { get; set; } = new List<SalesOrder>();
    public virtual ICollection<StockBalance> StockBalances { get; set; } = new List<StockBalance>();
    public virtual ICollection<StockMovement> StockMovements { get; set; } = new List<StockMovement>();
    public virtual ICollection<StockTransfer> StockTransferFromWarehouses { get; set; } = new List<StockTransfer>();
    public virtual ICollection<StockTransfer> StockTransferToWarehouses { get; set; } = new List<StockTransfer>();
    public virtual ICollection<WasteRecord> WasteRecords { get; set; } = new List<WasteRecord>();
}