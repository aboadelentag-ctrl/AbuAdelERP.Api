using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace KayanERP.Api.Models;

public partial class Company
{
    public long Id { get; set; }

    public string CompanyName { get; set; } = null!;

    public string CompanyCode { get; set; } = null!;

    // ==========================================================
    // بيانات دخول واعتماد المنشأة المستأجرة (Multi-Tenant SaaS)
    // ==========================================================
    [Column("tenant_code")]
    public string? TenantCode { get; set; }

    [Column("tenant_key")]
    public string? TenantKey { get; set; }

    public string? Phone { get; set; }

    public string? Email { get; set; }

    public string? Address { get; set; }

    public bool Active { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    // ==========================================================
    // علاقات الجداول والكيانات التابعة للشركة (Navigation Properties)
    // ==========================================================
    public virtual ICollection<AppUser> AppUsers { get; set; } = new List<AppUser>();

    public virtual ICollection<Branch> Branches { get; set; } = new List<Branch>();

    public virtual ICollection<Customer> Customers { get; set; } = new List<Customer>();

    public virtual ICollection<Department> Departments { get; set; } = new List<Department>();

    public virtual ICollection<ItemCategory> ItemCategories { get; set; } = new List<ItemCategory>();

    public virtual ICollection<Item> Items { get; set; } = new List<Item>();

    public virtual ICollection<MaintenanceAsset> MaintenanceAssets { get; set; } = new List<MaintenanceAsset>();

    public virtual ICollection<MaintenanceOrder> MaintenanceOrders { get; set; } = new List<MaintenanceOrder>();

    public virtual ICollection<ProductionOrder> ProductionOrders { get; set; } = new List<ProductionOrder>();

    public virtual ICollection<PurchaseOrder> PurchaseOrders { get; set; } = new List<PurchaseOrder>();

    public virtual ICollection<QualityInspection> QualityInspections { get; set; } = new List<QualityInspection>();

    public virtual ICollection<Recipe> Recipes { get; set; } = new List<Recipe>();

    public virtual ICollection<Role> Roles { get; set; } = new List<Role>();

    public virtual ICollection<SalesOrder> SalesOrders { get; set; } = new List<SalesOrder>();

    public virtual ICollection<StockBalance> StockBalances { get; set; } = new List<StockBalance>();

    public virtual ICollection<StockMovement> StockMovements { get; set; } = new List<StockMovement>();

    public virtual ICollection<StockTransfer> StockTransfers { get; set; } = new List<StockTransfer>();

    public virtual ICollection<Supplier> Suppliers { get; set; } = new List<Supplier>();

    public virtual ICollection<UnitConversion> UnitConversions { get; set; } = new List<UnitConversion>();

    public virtual ICollection<Unit> Units { get; set; } = new List<Unit>();

    public virtual ICollection<Warehouse> Warehouses { get; set; } = new List<Warehouse>();

    public virtual ICollection<WasteRecord> WasteRecords { get; set; } = new List<WasteRecord>();
}