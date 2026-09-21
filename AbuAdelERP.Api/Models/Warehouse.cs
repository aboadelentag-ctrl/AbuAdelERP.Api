using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace AbuAdelERP.Api.Models;

[Table("warehouses", Schema = "erp")]
public class Warehouse
{
    [Key]
    [Column("id")]
    public long Id { get; set; }

    [Column("company_id")]
    public long CompanyId { get; set; }

    [Column("branch_id")]
    public long? BranchId { get; set; }

    // --- الإضافة الجديدة: دعم المخازن المتفرعة (الشجرة) ---
    [Column("parent_warehouse_id")]
    public long? ParentWarehouseId { get; set; }

    [ForeignKey("ParentWarehouseId")]
    [JsonIgnore]
    public virtual Warehouse? ParentWarehouse { get; set; }

    [JsonIgnore]
    public virtual ICollection<Warehouse> ChildWarehouses { get; set; } = new List<Warehouse>();
    // ----------------------------------------------------

    [Column("warehouse_name")]
    public string WarehouseName { get; set; } = string.Empty;

    [Column("warehouse_code")]
    public string WarehouseCode { get; set; } = string.Empty;

    [Column("warehouse_type")]
    public string WarehouseType { get; set; } = string.Empty;

    [Column("description")]
    public string? Description { get; set; }

    [Column("active")]
    public bool Active { get; set; } = true;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTime? UpdatedAt { get; set; }

    // --- العلاقات الأخرى ---
    [JsonIgnore]
    public virtual ICollection<ProductionOrder> ProductionOrders { get; set; } = new List<ProductionOrder>();
    [JsonIgnore]
    public virtual ICollection<PurchaseOrder> PurchaseOrders { get; set; } = new List<PurchaseOrder>();
    [JsonIgnore]
    public virtual ICollection<QualityInspection> QualityInspections { get; set; } = new List<QualityInspection>();
    [JsonIgnore]
    public virtual ICollection<SalesOrder> SalesOrders { get; set; } = new List<SalesOrder>();
    [JsonIgnore]
    public virtual ICollection<StockBalance> StockBalances { get; set; } = new List<StockBalance>();
    [JsonIgnore]
    public virtual ICollection<StockMovement> StockMovements { get; set; } = new List<StockMovement>();

    [JsonIgnore]
    [InverseProperty("FromWarehouse")]
    public virtual ICollection<StockTransfer> StockTransferFromWarehouses { get; set; } = new List<StockTransfer>();

    [JsonIgnore]
    [InverseProperty("ToWarehouse")]
    public virtual ICollection<StockTransfer> StockTransferToWarehouses { get; set; } = new List<StockTransfer>();

    [JsonIgnore]
    public virtual ICollection<WasteRecord> WasteRecords { get; set; } = new List<WasteRecord>();
}