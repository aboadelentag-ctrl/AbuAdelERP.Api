using System;
using System.Collections.Generic;
using System.Linq.Expressions;              // ← جديد
using Microsoft.EntityFrameworkCore;
using KayanERP.Api.Models.Interfaces;      // ← جديد
using KayanERP.Api.Services;

namespace KayanERP.Api.Models;

public partial class AppDbContext : DbContext
{
    private readonly ITenantContext? _tenantContext;

    public AppDbContext()
    {
    }

    public AppDbContext(DbContextOptions<AppDbContext> options, ITenantContext? tenantContext = null)
        : base(options)
    {
        _tenantContext = tenantContext;
    }

    // ====== جداول الأصول والماكينات ودورة الأسبات ======
    public virtual DbSet<FixedAsset> FixedAssets { get; set; }
    public virtual DbSet<AssetHistoryLog> AssetHistoryLogs { get; set; }
    public virtual DbSet<CirculatingContainer> CirculatingContainers { get; set; }
    public virtual DbSet<CrateMovement> CrateMovements { get; set; }
    public virtual DbSet<CrateClient> CrateClients { get; set; }
    public virtual DbSet<CrateClientTransaction> CrateClientTransactions { get; set; }
    public virtual DbSet<Crate> Crates { get; set; }
    public virtual DbSet<CrateTransaction> CrateTransactions { get; set; }
    public virtual DbSet<AssetBalance> AssetBalances { get; set; }
    public virtual DbSet<AssetMovement> AssetMovements { get; set; }

    // ====== جداول البنية المؤسسية والمستخدمين ======
    public virtual DbSet<Company> Companies { get; set; }
    public virtual DbSet<Branch> Branches { get; set; }
    public virtual DbSet<Department> Departments { get; set; }
    public virtual DbSet<Employee> Employees { get; set; }
    public virtual DbSet<JobTitle> JobTitles { get; set; }
    public virtual DbSet<AppRole> Roles { get; set; }
    public virtual DbSet<AppUser> Users { get; set; }
    public virtual DbSet<RefreshToken> RefreshTokens { get; set; }
    public virtual DbSet<ProductivityLog> ProductivityLogs { get; set; }
    public virtual DbSet<BranchTransfer> BranchTransfers { get; set; }

    // ====== النماذج الديناميكية والطوارئ ======
    public virtual DbSet<DynamicForm> DynamicForms { get; set; }
    public virtual DbSet<FormSubmission> FormSubmissions { get; set; }
    public virtual DbSet<FormSubmission> DynamicFormSubmissions => FormSubmissions;
    public virtual DbSet<EmergencyTask> EmergencyTasks { get; set; }

    // ====== بطاقات الأصناف والوحدات ======
    public virtual DbSet<Item> Items { get; set; }
    public virtual DbSet<ItemCategory> ItemCategories { get; set; }
    public virtual DbSet<ItemUomConversion> ItemUomConversions { get; set; }
    public virtual DbSet<Unit> Units { get; set; }
    public virtual DbSet<UnitConversion> UnitConversions { get; set; }

    // ====== إدارة التصنيع والوصفات (BOM) ======
    public virtual DbSet<Recipe> Recipes { get; set; }
    public virtual DbSet<RecipeLine> RecipeLines { get; set; }
    public virtual DbSet<RecipeStage> RecipeStages { get; set; }
    public virtual DbSet<ProductionOrder> ProductionOrders { get; set; }
    public virtual DbSet<ProductionOrderDetail> ProductionOrderDetails { get; set; }
    public virtual DbSet<ProductionOrderStage> ProductionOrderStages { get; set; }
    public virtual DbSet<ProductionStageMaterial> ProductionStageMaterials { get; set; }
    public virtual DbSet<QualityInspection> QualityInspections { get; set; }
    public virtual DbSet<WasteRecord> WasteRecords { get; set; }

    // ====== المشتريات والموردين ======
    public virtual DbSet<Supplier> Suppliers { get; set; }
    public virtual DbSet<SupplierItemPrice> SupplierItemPrices { get; set; } = null!;
    public virtual DbSet<PurchaseOrder> PurchaseOrders { get; set; }
    public virtual DbSet<PurchaseOrderLine> PurchaseOrderLines { get; set; }
    public virtual DbSet<PurchaseInvoice> PurchaseInvoices { get; set; }
    public virtual DbSet<PurchaseInvoiceLine> PurchaseInvoiceLines { get; set; }

    // ====== المستودعات وحركات المخزون ======
    public virtual DbSet<Warehouse> Warehouses { get; set; }
    public virtual DbSet<StockBalance> StockBalances { get; set; }
    public virtual DbSet<StockMovement> StockMovements { get; set; }
    public virtual DbSet<StockTransfer> StockTransfers { get; set; }
    public virtual DbSet<StockTransferLine> StockTransferLines { get; set; }
    public virtual DbSet<MaterialRequisition> MaterialRequisitions { get; set; }
    public virtual DbSet<MaterialRequisitionLine> MaterialRequisitionLines { get; set; }
    public virtual DbSet<FinishedGoodsTransfer> FinishedGoodsTransfers { get; set; }

    // ====== المبيعات والعملاء (Sales Module) ======
    public virtual DbSet<Customer> Customers { get; set; }
    public virtual DbSet<SalesOrder> SalesOrders { get; set; }
    public virtual DbSet<SalesOrderLine> SalesOrderLines { get; set; }
    public virtual DbSet<SalesInvoice> SalesInvoices { get; set; }
    public virtual DbSet<SalesInvoiceLine> SalesInvoiceLines { get; set; }

    // ====== المالية والحسابات ======
    public virtual DbSet<Account> Accounts { get; set; }
    public virtual DbSet<JournalEntry> JournalEntries { get; set; }
    public virtual DbSet<JournalEntryLine> JournalEntryLines { get; set; }

    // ====== الصيانة ======
    public virtual DbSet<MaintenanceAsset> MaintenanceAssets { get; set; }
    public virtual DbSet<MaintenanceOrder> MaintenanceOrders { get; set; }

    // ====== الـ Views المدمجة ======
    public virtual DbSet<VCoreItem> VCoreItems { get; set; }
    public virtual DbSet<VMaterialShortage> VMaterialShortages { get; set; }
    public virtual DbSet<VMultiLevelRecipe> VMultiLevelRecipes { get; set; }
    public virtual DbSet<VProductionOrderDetail> VProductionOrderDetails { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // ----------------------------------------------------
        // 0. تثبيت المخطط الافتراضي لجميع الكيانات في Schema الـ erp
        // ----------------------------------------------------
        modelBuilder.HasDefaultSchema("erp");

       
        // ----------------------------------------------------
        // 1. المستخدمين (Users / AppUser)
        // ----------------------------------------------------
        modelBuilder.Entity<AppUser>(entity =>
        {
            entity.ToTable("users");
        });

        // ----------------------------------------------------
        // Refresh Tokens — لتجديد Access Tokens بدون Login
        // ----------------------------------------------------
        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("refresh_tokens_pkey");
            entity.ToTable("refresh_tokens");

            // فهارس مهمة للأداء
            entity.HasIndex(e => e.Token, "ux_refresh_tokens_token").IsUnique();
            entity.HasIndex(e => e.UserId, "idx_refresh_tokens_user_id");
            entity.HasIndex(e => e.ExpiresAt, "idx_refresh_tokens_expires_at");

            // Properties
            entity.Property(e => e.Id)
                .UseIdentityAlwaysColumn()
                .HasColumnName("id");

            entity.Property(e => e.UserId)
                .HasColumnName("user_id");

            entity.Property(e => e.Token)
                .HasMaxLength(500)
                .HasColumnName("token");

            entity.Property(e => e.ExpiresAt)
                .HasColumnName("expires_at");

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("created_at");

            entity.Property(e => e.RevokedAt)
                .HasColumnName("revoked_at");

            entity.Property(e => e.ReplacedByToken)
                .HasMaxLength(500)
                .HasColumnName("replaced_by_token");

            entity.Property(e => e.RevokedReason)
                .HasMaxLength(100)
                .HasColumnName("revoked_reason");

            entity.Property(e => e.CreatedByIp)
                .HasMaxLength(50)
                .HasColumnName("created_by_ip");

            // علاقة مع AppUser
            entity.HasOne(d => d.User)
                .WithMany()
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("refresh_tokens_user_id_fkey");
        });

        // ----------------------------------------------------
        // Crate Clients — عهد الأسبات مع العملاء
        // ----------------------------------------------------
        modelBuilder.Entity<CrateClient>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("crate_clients_pkey");
            entity.ToTable("crate_clients");

            entity.HasIndex(e => e.CompanyId, "ix_crate_clients_company_id");
            entity.HasIndex(e => new { e.CompanyId, e.Name }, "crate_clients_company_id_name_key").IsUnique();

            entity.Property(e => e.Id)
                .UseIdentityAlwaysColumn()
                .HasColumnName("id");

            entity.Property(e => e.CompanyId)
                .HasColumnName("company_id");

            entity.Property(e => e.Name)
                .HasMaxLength(200)
                .HasColumnName("name");

            entity.Property(e => e.CrateBalance)
                .HasDefaultValue(0)
                .HasColumnName("crate_balance");

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("created_at");

            entity.HasOne(d => d.Company)
                .WithMany()
                .HasForeignKey(d => d.CompanyId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("crate_clients_company_id_fkey");
        });

               // ----------------------------------------------------
        // Crate Clients — عهد الأسبات مع العملاء
        // ----------------------------------------------------
        modelBuilder.Entity<CrateClient>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("crate_clients_pkey");
            entity.ToTable("crate_clients");

            entity.HasIndex(e => e.CompanyId, "ix_crate_clients_company_id");
            entity.HasIndex(e => new { e.CompanyId, e.Name }, "crate_clients_company_id_name_key").IsUnique();

            entity.Property(e => e.Id)
                .UseIdentityAlwaysColumn()
                .HasColumnName("id");

            entity.Property(e => e.CompanyId)
                .HasColumnName("company_id");

            entity.Property(e => e.Name)
                .HasMaxLength(200)
                .HasColumnName("name");

            entity.Property(e => e.CrateBalance)
                .HasDefaultValue(0)
                .HasColumnName("crate_balance");

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("created_at");

            entity.HasOne(d => d.Company)
                .WithMany()
                .HasForeignKey(d => d.CompanyId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("crate_clients_company_id_fkey");
        });

        // ----------------------------------------------------
        // Crate Client Transactions — حركات عهد العملاء
        // ----------------------------------------------------
        modelBuilder.Entity<CrateClientTransaction>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("crate_client_transactions_pkey");
            entity.ToTable("crate_client_transactions");

            entity.HasIndex(e => e.CompanyId, "ix_crate_client_transactions_company_id");
            entity.HasIndex(e => e.CrateClientId, "ix_crate_client_transactions_crate_client_id");
            entity.HasIndex(e => e.TransactionDate, "ix_crate_client_transactions_date");

            entity.Property(e => e.Id)
                .UseIdentityAlwaysColumn()
                .HasColumnName("id");

            entity.Property(e => e.CompanyId)
                .HasColumnName("company_id");

            entity.Property(e => e.CrateClientId)
                .HasColumnName("crate_client_id");

            entity.Property(e => e.TransactionType)
                .HasMaxLength(20)
                .HasColumnName("transaction_type");

            entity.Property(e => e.Quantity)
                .HasColumnName("quantity");

            entity.Property(e => e.TransactionDate)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("transaction_date");

            entity.Property(e => e.Notes)
                .HasMaxLength(500)
                .HasColumnName("notes");

            entity.HasOne(d => d.CrateClient)
                .WithMany(p => p.Transactions)
                .HasForeignKey(d => d.CrateClientId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("crate_client_transactions_crate_client_id_fkey");

            entity.HasOne(d => d.Company)
                .WithMany()
                .HasForeignKey(d => d.CompanyId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("crate_client_transactions_company_id_fkey");
        });

        // ----------------------------------------------------
        // 2. الفروع (Branches)
        // ----------------------------------------------------
        modelBuilder.Entity<Branch>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("branches_pkey");
            entity.ToTable("branches");
            entity.HasIndex(e => new { e.CompanyId, e.BranchCode }, "branches_company_id_branch_code_key").IsUnique();
            entity.HasIndex(e => e.CompanyId, "ix_branches_company_id");

            entity.Property(e => e.Id).UseIdentityAlwaysColumn().HasColumnName("id");
            entity.Property(e => e.CompanyId).HasColumnName("company_id");
            entity.Property(e => e.BranchCode).HasMaxLength(50).HasColumnName("branch_code");
            entity.Property(e => e.BranchName).HasMaxLength(200).HasColumnName("branch_name");
            entity.Property(e => e.Address).HasColumnName("address");
            entity.Property(e => e.Phone).HasMaxLength(50).HasColumnName("phone");
            entity.Property(e => e.Active).HasDefaultValue(true).HasColumnName("active");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP").HasColumnName("created_at");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP").HasColumnName("updated_at");

            entity.HasOne(d => d.Company)
                .WithMany(p => p.Branches)
                .HasForeignKey(d => d.CompanyId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("branches_company_id_fkey");
        });

        // ----------------------------------------------------
        // 3. الشركات (Companies)
        // ----------------------------------------------------
        modelBuilder.Entity<Company>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("companies_pkey");
            entity.ToTable("companies");
            entity.HasIndex(e => e.CompanyCode, "companies_company_code_key").IsUnique();

            entity.Property(e => e.Id).UseIdentityAlwaysColumn().HasColumnName("id");
            entity.Property(e => e.Active).HasDefaultValue(true).HasColumnName("active");
            entity.Property(e => e.Address).HasColumnName("address");
            entity.Property(e => e.CompanyCode).HasMaxLength(50).HasColumnName("company_code");
            entity.Property(e => e.CompanyName).HasMaxLength(200).HasColumnName("company_name");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP").HasColumnName("created_at");
            entity.Property(e => e.Email).HasMaxLength(150).HasColumnName("email");
            entity.Property(e => e.Phone).HasMaxLength(50).HasColumnName("phone");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP").HasColumnName("updated_at");
        });

        // ----------------------------------------------------
        // 4. المستودعات (Warehouses)
        // ----------------------------------------------------
        modelBuilder.Entity<Warehouse>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("warehouses_pkey");
            entity.ToTable("warehouses");
            entity.HasIndex(e => new { e.CompanyId, e.WarehouseCode }, "ux_warehouses_company_code").IsUnique();
            entity.HasIndex(e => e.BranchId, "idx_warehouses_branch_id");

            entity.Property(e => e.Id).UseIdentityAlwaysColumn().HasColumnName("id");
            entity.Property(e => e.CompanyId).HasColumnName("company_id");
            entity.Property(e => e.BranchId).HasColumnName("branch_id");
            entity.Property(e => e.WarehouseCode).HasMaxLength(50).HasColumnName("warehouse_code");
            entity.Property(e => e.WarehouseName).HasMaxLength(200).HasColumnName("warehouse_name");
            entity.Property(e => e.WarehouseType).HasMaxLength(50).HasColumnName("warehouse_type");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.ParentWarehouseId).HasColumnName("parent_warehouse_id");
            entity.Property(e => e.Active).HasDefaultValue(true).HasColumnName("active");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP").HasColumnName("created_at");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP").HasColumnName("updated_at");

            entity.HasOne(d => d.Branch)
                .WithMany(p => p.Warehouses)
                .HasForeignKey(d => d.BranchId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("warehouses_branch_id_fkey");

            entity.HasOne(d => d.ParentWarehouse)
                .WithMany(p => p.InverseParentWarehouse)
                .HasForeignKey(d => d.ParentWarehouseId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("warehouses_parent_warehouse_id_fkey");
        });

        // ----------------------------------------------------
        // 5. فواتير المبيعات (SalesInvoices)
        // ----------------------------------------------------
        modelBuilder.Entity<SalesInvoice>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("sales_invoices_pkey");
            entity.ToTable("sales_invoices");

            entity.HasIndex(e => e.InvoiceNumber, "ux_sales_invoices_invoice_number").IsUnique();
            entity.HasIndex(e => e.BranchId, "idx_sales_invoices_branch_id");
            entity.HasIndex(e => e.CustomerId, "idx_sales_invoices_customer_id");
            entity.HasIndex(e => e.InvoiceDate, "idx_sales_invoices_date");

            entity.Property(e => e.Id).UseIdentityAlwaysColumn().HasColumnName("id");
            entity.Property(e => e.CompanyId).HasColumnName("company_id");
            entity.Property(e => e.BranchId).HasColumnName("branch_id");
            entity.Property(e => e.InvoiceNumber).HasMaxLength(100).HasColumnName("invoice_number");
            entity.Property(e => e.InvoiceDate).HasColumnName("invoice_date");
            entity.Property(e => e.CustomerId).HasColumnName("customer_id");
            entity.Property(e => e.TotalAmount).HasPrecision(18, 2).HasColumnName("total_amount");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP").HasColumnName("created_at");

            entity.HasOne<Branch>()
                .WithMany()
                .HasForeignKey(d => d.BranchId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("sales_invoices_branch_id_fkey");

            entity.HasOne<Customer>()
                .WithMany()
                .HasForeignKey(d => d.CustomerId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("sales_invoices_customer_id_fkey");
        });

        // ----------------------------------------------------
        // 6. بنود فواتير المبيعات (SalesInvoiceLines)
        // ----------------------------------------------------
        modelBuilder.Entity<SalesInvoiceLine>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("sales_invoice_lines_pkey");
            entity.ToTable("sales_invoice_lines");

            entity.HasIndex(e => e.InvoiceId, "idx_sales_invoice_lines_invoice_id");
            entity.HasIndex(e => e.ItemId, "idx_sales_invoice_lines_item_id");

            entity.Property(e => e.Id).UseIdentityAlwaysColumn().HasColumnName("id");
            entity.Property(e => e.InvoiceId).HasColumnName("invoice_id");
            entity.Property(e => e.ItemId).HasColumnName("item_id");
            entity.Property(e => e.Quantity).HasColumnName("quantity");
            entity.Property(e => e.UnitPrice).HasPrecision(18, 2).HasColumnName("unit_price");
            entity.Property(e => e.LineTotal).HasPrecision(18, 2).HasColumnName("line_total");

            entity.HasOne(d => d.Invoice)
                .WithMany(p => p.InvoiceLines)
                .HasForeignKey(d => d.InvoiceId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("sales_invoice_lines_invoice_id_fkey");

            entity.HasOne(d => d.Item)
                .WithMany()
                .HasForeignKey(d => d.ItemId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("sales_invoice_lines_item_id_fkey");
        });

        // ----------------------------------------------------
        // 7. العملاء (Customers)
        // ----------------------------------------------------
        modelBuilder.Entity<Customer>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("customers_pkey");
            entity.ToTable("customers");
            entity.HasIndex(e => new { e.CompanyId, e.CustomerCode }, "customers_company_id_customer_code_key").IsUnique();
            entity.HasIndex(e => e.CompanyId, "ix_customers_company_id");

            entity.Property(e => e.Id).UseIdentityAlwaysColumn().HasColumnName("id");
            entity.Property(e => e.Active).HasDefaultValue(true).HasColumnName("active");
            entity.Property(e => e.Address).HasColumnName("address");
            entity.Property(e => e.CompanyId).HasColumnName("company_id");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP").HasColumnName("created_at");
            entity.Property(e => e.CreditLimit).HasPrecision(18, 2).HasColumnName("credit_limit");
            entity.Property(e => e.CustomerCode).HasMaxLength(50).HasColumnName("customer_code");
            entity.Property(e => e.CustomerName).HasMaxLength(200).HasColumnName("customer_name");
            entity.Property(e => e.CustomerType).HasMaxLength(30).HasDefaultValueSql("'RETAIL'").HasColumnName("customer_type");
            entity.Property(e => e.Email).HasMaxLength(150).HasColumnName("email");
            entity.Property(e => e.Notes).HasColumnName("notes");
            entity.Property(e => e.PaymentTerms).HasMaxLength(100).HasColumnName("payment_terms");
            entity.Property(e => e.Phone).HasMaxLength(50).HasColumnName("phone");
            entity.Property(e => e.TaxNumber).HasMaxLength(100).HasColumnName("tax_number");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP").HasColumnName("updated_at");

            entity.HasOne(d => d.Company).WithMany(p => p.Customers)
                .HasForeignKey(d => d.CompanyId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("customers_company_id_fkey");
        });

        // ----------------------------------------------------
        // 8. الأقسام والموظفين (Departments & Employees)
        // ----------------------------------------------------
        modelBuilder.Entity<Department>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("departments_pkey");
            entity.ToTable("departments");
            entity.HasIndex(e => new { e.CompanyId, e.DepartmentCode }, "departments_company_id_department_code_key").IsUnique();

            entity.HasOne(d => d.ParentDepartment)
                .WithMany(p => p.SubDepartments)
                .HasForeignKey(d => d.ParentDepartmentId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("departments_parent_id_fkey");

            entity.HasOne(d => d.Company).WithMany(p => p.Departments)
                .HasForeignKey(d => d.CompanyId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("departments_company_id_fkey");
        });

        modelBuilder.Entity<Employee>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("employees_pkey");
            entity.ToTable("employees");

            entity.HasOne(e => e.Manager)
                .WithMany()
                .HasForeignKey(e => e.ManagerId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("employees_manager_id_fkey");

            entity.HasOne(e => e.Department)
                .WithMany(d => d.Employees)
                .HasForeignKey(e => e.DepartmentId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("employees_department_id_fkey");
        });

        modelBuilder.Entity<JobTitle>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("job_titles_pkey");
            entity.ToTable("job_titles");
            entity.HasIndex(e => new { e.CompanyId, e.TitleName }, "ux_job_titles_company_title").IsUnique();
        });

        // ----------------------------------------------------
        // 9. بطاقات الأصناف والتصنيفات (Items)
        // ----------------------------------------------------
        modelBuilder.Entity<Item>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("items_pkey");
            entity.ToTable("items");
            entity.HasIndex(e => new { e.CompanyId, e.ItemCode }, "items_company_id_item_code_key").IsUnique();
            entity.HasIndex(e => e.CategoryId, "idx_items_category_id");
            entity.HasIndex(e => e.DepartmentId, "idx_items_department_id");

            entity.Property(e => e.Id).UseIdentityAlwaysColumn().HasColumnName("id");
            entity.Property(e => e.CompanyId).HasColumnName("company_id");
            entity.Property(e => e.CategoryId).HasColumnName("category_id");
            entity.Property(e => e.DepartmentId).HasColumnName("department_id");
            entity.Property(e => e.DepartmentName).HasColumnName("department_name");
            entity.Property(e => e.ItemCode).HasMaxLength(100).HasColumnName("item_code");
            entity.Property(e => e.ItemName).HasMaxLength(250).HasColumnName("item_name");
            entity.Property(e => e.ItemType).HasMaxLength(30).HasColumnName("item_type");
            entity.Property(e => e.Unit).HasMaxLength(50).HasColumnName("unit");
            entity.Property(e => e.ParentItemId).HasColumnName("parent_item_id");
            entity.Property(e => e.HasExpiryDate).HasDefaultValue(true).HasColumnName("has_expiry_date");
            entity.Property(e => e.ShelfLifeDays).HasColumnName("shelf_life_days");
            entity.Property(e => e.ReorderLevel).HasPrecision(18, 3).HasColumnName("reorder_level");
            entity.Property(e => e.SafetyStock).HasPrecision(18, 3).HasColumnName("safety_stock");
            entity.Property(e => e.MaxStock).HasPrecision(18, 3).HasColumnName("max_stock");
            entity.Property(e => e.Active).HasDefaultValue(true).HasColumnName("active");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP").HasColumnName("created_at");
            entity.Property(e => e.BasePrice).HasPrecision(18, 2).HasColumnName("base_price");
            entity.Property(e => e.Barcode).HasMaxLength(100).HasColumnName("barcode");
            entity.Property(e => e.IsVariant).HasDefaultValue(false).HasColumnName("is_variant");
            entity.Property(e => e.IsMerged).HasDefaultValue(false).HasColumnName("is_merged");
            entity.Property(e => e.IsContainer).HasDefaultValue(false).HasColumnName("is_container");
            entity.Property(e => e.MultiUom).HasDefaultValue(false).HasColumnName("multi_uom");
            entity.Property(e => e.HideDetailPrint).HasDefaultValue(false).HasColumnName("hide_detail_print");

            entity.HasOne(d => d.ItemCategory)
                .WithMany(p => p.Items)
                .HasForeignKey(d => d.CategoryId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("items_category_id_fkey");

            entity.HasOne(d => d.Department)
                .WithMany()
                .HasForeignKey(d => d.DepartmentId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("items_department_id_fkey");

            entity.HasOne(d => d.ParentItem)
                .WithMany(p => p.Variants)
                .HasForeignKey(d => d.ParentItemId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("items_parent_item_id_fkey");
        });

        modelBuilder.Entity<ItemCategory>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("item_categories_pkey");
            entity.ToTable("item_categories");
            entity.HasIndex(e => new { e.CompanyId, e.CategoryCode }, "item_categories_company_id_category_code_key").IsUnique();
            entity.HasIndex(e => e.CompanyId, "ix_item_categories_company_id");

            entity.Property(e => e.Id).UseIdentityAlwaysColumn().HasColumnName("id");
            entity.Property(e => e.Active).HasDefaultValue(true).HasColumnName("active");
            entity.Property(e => e.CategoryCode).HasMaxLength(50).HasColumnName("category_code");
            entity.Property(e => e.CategoryName).HasMaxLength(200).HasColumnName("category_name");
            entity.Property(e => e.CompanyId).HasColumnName("company_id");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP").HasColumnName("created_at");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.ItemType).HasMaxLength(30).HasColumnName("item_type");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP").HasColumnName("updated_at");

            entity.HasOne(d => d.Company).WithMany(p => p.ItemCategories)
                .HasForeignKey(d => d.CompanyId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("item_categories_company_id_fkey");
        });

        // ----------------------------------------------------
        // 10. أوامر الإنتاج والتكاليف الصناعية
        // ----------------------------------------------------
        modelBuilder.Entity<ProductionOrder>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("production_orders_pkey");
            entity.ToTable("production_orders");
            entity.HasIndex(e => e.Status, "idx_production_orders_status");
            entity.HasIndex(e => new { e.CompanyId, e.OrderNumber }, "production_orders_company_order_number_unique").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CompanyId).HasColumnName("company_id");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP").HasColumnName("created_at");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");
            entity.Property(e => e.Notes).HasColumnName("notes");
            entity.Property(e => e.OrderDate).HasDefaultValueSql("CURRENT_DATE").HasColumnName("order_date");
            entity.Property(e => e.OrderNumber).HasMaxLength(50).HasColumnName("order_number");
            entity.Property(e => e.Status).HasMaxLength(30).HasDefaultValueSql("'DRAFT'").HasColumnName("status");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP").HasColumnName("updated_at");
            entity.Property(e => e.WarehouseId).HasColumnName("warehouse_id");

            entity.HasOne(d => d.Company).WithMany(p => p.ProductionOrders)
                .HasForeignKey(d => d.CompanyId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("production_orders_company_fk");

            entity.HasOne(d => d.Warehouse).WithMany(p => p.ProductionOrders)
                .HasForeignKey(d => d.WarehouseId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("production_orders_warehouse_fk");
        });

        modelBuilder.Entity<ProductionOrderDetail>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("production_order_details_pkey");
            entity.ToTable("production_order_details");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.ProductionOrderId).HasColumnName("production_order_id");
            entity.Property(e => e.ItemId).HasColumnName("item_id");
            entity.Property(e => e.RecipeId).HasColumnName("recipe_id");
            entity.Property(e => e.PlannedQuantity).HasPrecision(14, 3).HasColumnName("planned_quantity");
            entity.Property(e => e.ProducedQuantity).HasPrecision(14, 3).HasColumnName("produced_quantity");
            entity.Property(e => e.Status).HasMaxLength(30).HasDefaultValueSql("'PENDING'").HasColumnName("status");
            entity.Property(e => e.Notes).HasColumnName("notes");
            entity.Property(e => e.EmployeeId).HasColumnName("Employee_id");
            entity.Property(e => e.WorkingHours).HasPrecision(10, 2).HasColumnName("working_hours");
            entity.Property(e => e.WasteQuantity).HasPrecision(14, 3).HasColumnName("waste_quantity");

            entity.HasOne(d => d.ProductionOrder).WithMany(p => p.ProductionOrderDetails)
                .HasForeignKey(d => d.ProductionOrderId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("production_order_details_order_fk");

            entity.HasOne(d => d.Item).WithMany()
                .HasForeignKey(d => d.ItemId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("production_order_details_item_fk");

            entity.HasOne(d => d.Recipe).WithMany()
                .HasForeignKey(d => d.RecipeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("production_order_details_recipe_fk");
        });

        modelBuilder.Entity<ProductionOrderStage>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("production_order_stages_pkey");
            entity.ToTable("production_order_stages");
            entity.HasIndex(e => e.ItemId, "idx_production_stages_item");
            entity.HasIndex(e => e.ProductionOrderDetailId, "idx_production_stages_detail");
            entity.HasIndex(e => e.RecipeStageId, "idx_production_stages_recipe_stage");
            entity.HasIndex(e => new { e.ProductionOrderDetailId, e.StageNumber }, "production_stages_unique").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP").HasColumnName("created_at");
            entity.Property(e => e.ItemId).HasColumnName("item_id");
            entity.Property(e => e.Notes).HasColumnName("notes");
            entity.Property(e => e.PlannedQuantity).HasPrecision(14, 3).HasColumnName("planned_quantity");
            entity.Property(e => e.ProducedQuantity).HasPrecision(14, 3).HasColumnName("produced_quantity");
            entity.Property(e => e.ProductionOrderDetailId).HasColumnName("production_order_detail_id");
            entity.Property(e => e.RecipeStageId).HasColumnName("recipe_stage_id");
            entity.Property(e => e.StageNumber).HasColumnName("stage_number");
            entity.Property(e => e.Status).HasMaxLength(30).HasDefaultValueSql("'PENDING'").HasColumnName("status");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP").HasColumnName("updated_at");

            entity.HasOne(d => d.Item).WithMany()
                .HasForeignKey(d => d.ItemId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("production_stages_item_fk");

            entity.HasOne(d => d.ProductionOrderDetail).WithMany(p => p.ProductionOrderStages)
                .HasForeignKey(d => d.ProductionOrderDetailId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("production_stages_detail_fk");

            entity.HasOne(d => d.RecipeStage).WithMany(p => p.ProductionOrderStages)
                .HasForeignKey(d => d.RecipeStageId)
                .HasConstraintName("production_stages_recipe_stage_fk");
        });

        modelBuilder.Entity<ProductionStageMaterial>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("production_stage_materials_pkey");
            entity.ToTable("production_stage_materials");
            entity.HasIndex(e => e.RecipeLineId, "idx_production_material_recipe_line");
            entity.HasIndex(e => e.ItemId, "idx_production_materials_item");
            entity.HasIndex(e => e.ProductionStageId, "idx_production_materials_stage");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.ConsumedQuantity).HasPrecision(14, 3).HasColumnName("consumed_quantity");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP").HasColumnName("created_at");
            entity.Property(e => e.ItemId).HasColumnName("item_id");
            entity.Property(e => e.Notes).HasColumnName("notes");
            entity.Property(e => e.ProductionStageId).HasColumnName("production_stage_id");
            entity.Property(e => e.RecipeLineId).HasColumnName("recipe_line_id");
            entity.Property(e => e.RequiredQuantity).HasPrecision(14, 3).HasColumnName("required_quantity");
            entity.Property(e => e.Unit).HasMaxLength(50).HasColumnName("unit");
            entity.Property(e => e.WastePercentage).HasPrecision(8, 3).HasColumnName("waste_percentage");

            entity.HasOne(d => d.Item).WithMany()
                .HasForeignKey(d => d.ItemId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("production_materials_item_fk");

            entity.HasOne(d => d.ProductionStage).WithMany(p => p.ProductionStageMaterials)
                .HasForeignKey(d => d.ProductionStageId)
                .HasConstraintName("production_materials_stage_fk");

            entity.HasOne(d => d.RecipeLine).WithMany(p => p.ProductionStageMaterials)
                .HasForeignKey(d => d.RecipeLineId)
                .HasConstraintName("production_material_recipe_line_fk");
        });

        // ----------------------------------------------------
        // 11. أوامر الشراء والموردين
        // ----------------------------------------------------
        modelBuilder.Entity<PurchaseOrder>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("purchase_orders_pkey");
            entity.ToTable("purchase_orders");
            entity.HasIndex(e => new { e.CompanyId, e.OrderNumber }, "purchase_orders_company_id_order_number_key").IsUnique();

            entity.Property(e => e.Id).UseIdentityAlwaysColumn().HasColumnName("id");
            entity.Property(e => e.CompanyId).HasColumnName("company_id");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP").HasColumnName("created_at");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");
            entity.Property(e => e.DiscountAmount).HasPrecision(18, 2).HasColumnName("discount_amount");
            entity.Property(e => e.ExpectedDate).HasColumnName("expected_date");
            entity.Property(e => e.Notes).HasColumnName("notes");
            entity.Property(e => e.OrderDate).HasDefaultValueSql("CURRENT_DATE").HasColumnName("order_date");
            entity.Property(e => e.OrderNumber).HasMaxLength(50).HasColumnName("order_number");
            entity.Property(e => e.Status).HasMaxLength(30).HasDefaultValueSql("'DRAFT'").HasColumnName("status");
            entity.Property(e => e.Subtotal).HasPrecision(18, 2).HasColumnName("subtotal");
            entity.Property(e => e.SupplierId).HasColumnName("supplier_id");
            entity.Property(e => e.TaxAmount).HasPrecision(18, 2).HasColumnName("tax_amount");
            entity.Property(e => e.TotalAmount).HasPrecision(18, 2).HasColumnName("total_amount");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP").HasColumnName("updated_at");
            entity.Property(e => e.WarehouseId).HasColumnName("warehouse_id");

            entity.HasOne(d => d.Company).WithMany(p => p.PurchaseOrders)
                .HasForeignKey(d => d.CompanyId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("purchase_orders_company_id_fkey");

            entity.HasOne(d => d.Supplier).WithMany(p => p.PurchaseOrders)
                .HasForeignKey(d => d.SupplierId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("purchase_orders_supplier_id_fkey");

            entity.HasOne(d => d.Warehouse).WithMany(p => p.PurchaseOrders)
                .HasForeignKey(d => d.WarehouseId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("purchase_orders_warehouse_id_fkey");
        });

        modelBuilder.Entity<PurchaseOrderLine>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("purchase_order_lines_pkey");
            entity.ToTable("purchase_order_lines");

            entity.Property(e => e.Id).UseIdentityAlwaysColumn().HasColumnName("id");
            entity.Property(e => e.DiscountAmount).HasPrecision(18, 2).HasColumnName("discount_amount");
            entity.Property(e => e.ItemId).HasColumnName("item_id");
            entity.Property(e => e.LineTotal).HasPrecision(18, 2).HasComputedColumnSql("(((quantity * unit_price) - discount_amount) + tax_amount)", true).HasColumnName("line_total");
            entity.Property(e => e.Notes).HasColumnName("notes");
            entity.Property(e => e.PurchaseOrderId).HasColumnName("purchase_order_id");
            entity.Property(e => e.Quantity).HasPrecision(18, 3).HasColumnName("quantity");
            entity.Property(e => e.ReceivedQuantity).HasPrecision(18, 3).HasColumnName("received_quantity");
            entity.Property(e => e.TaxAmount).HasPrecision(18, 2).HasColumnName("tax_amount");
            entity.Property(e => e.UnitPrice).HasPrecision(18, 4).HasColumnName("unit_price");

            entity.HasOne(d => d.Item).WithMany()
                .HasForeignKey(d => d.ItemId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("purchase_order_lines_item_id_fkey");

            entity.HasOne(d => d.PurchaseOrder).WithMany(p => p.PurchaseOrderLines)
                .HasForeignKey(d => d.PurchaseOrderId)
                .HasConstraintName("purchase_order_lines_purchase_order_id_fkey");
        });

        // ----------------------------------------------------
        // 12. الرقابة على الجودة وفحص الخامات
        // ----------------------------------------------------
        modelBuilder.Entity<QualityInspection>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("quality_inspections_pkey");
            entity.ToTable("quality_inspections");
            entity.HasIndex(e => e.ItemId, "idx_quality_item");
            entity.HasIndex(e => e.ProductionOrderId, "idx_quality_production");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CompanyId).HasColumnName("company_id");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP").HasColumnName("created_at");
            entity.Property(e => e.InspectionDate).HasDefaultValueSql("CURRENT_TIMESTAMP").HasColumnName("inspection_date");
            entity.Property(e => e.InspectionType).HasMaxLength(30).HasColumnName("inspection_type");
            entity.Property(e => e.ItemId).HasColumnName("item_id");
            entity.Property(e => e.Notes).HasColumnName("notes");
            entity.Property(e => e.ProductionOrderId).HasColumnName("production_order_id");
            entity.Property(e => e.ProductionStageId).HasColumnName("production_stage_id");
            entity.Property(e => e.Quantity).HasPrecision(18, 6).HasColumnName("quantity");
            entity.Property(e => e.Result).HasColumnName("result");
            entity.Property(e => e.Status).HasMaxLength(30).HasDefaultValueSql("'PENDING'").HasColumnName("status");
            entity.Property(e => e.WarehouseId).HasColumnName("warehouse_id");

            entity.HasOne(d => d.Company).WithMany(p => p.QualityInspections)
                .HasForeignKey(d => d.CompanyId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("quality_company_fk");

            entity.HasOne(d => d.Item).WithMany()
                .HasForeignKey(d => d.ItemId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("quality_inspections_item_id_fkey");

            entity.HasOne(d => d.ProductionOrder).WithMany()
                .HasForeignKey(d => d.ProductionOrderId)
                .HasConstraintName("quality_inspections_production_order_id_fkey");

            entity.HasOne(d => d.ProductionStage).WithMany()
                .HasForeignKey(d => d.ProductionStageId)
                .HasConstraintName("quality_inspections_production_stage_id_fkey");

            entity.HasOne(d => d.Warehouse).WithMany(p => p.QualityInspections)
                .HasForeignKey(d => d.WarehouseId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("quality_inspections_warehouse_id_fkey");
        });

        // ----------------------------------------------------
        // 13. وصفات التصنيع ومراحل التشغيل (Recipes)
        // ----------------------------------------------------
        modelBuilder.Entity<Recipe>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("recipes_pkey");
            entity.ToTable("recipes");
            entity.HasIndex(e => new { e.CompanyId, e.RecipeCode }, "recipes_company_id_recipe_code_key").IsUnique();

            entity.Property(e => e.Id).UseIdentityAlwaysColumn().HasColumnName("id");
            entity.Property(e => e.Active).HasDefaultValue(true).HasColumnName("active");
            entity.Property(e => e.BatchQuantity).HasPrecision(18, 3).HasColumnName("batch_quantity");
            entity.Property(e => e.BatchUnit).HasMaxLength(50).HasColumnName("batch_unit");
            entity.Property(e => e.CompanyId).HasColumnName("company_id");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP").HasColumnName("created_at");
            entity.Property(e => e.EffectiveFrom).HasDefaultValueSql("CURRENT_DATE").HasColumnName("effective_from");
            entity.Property(e => e.EffectiveTo).HasColumnName("effective_to");
            entity.Property(e => e.Notes).HasColumnName("notes");
            entity.Property(e => e.PiecesPerBatch).HasPrecision(18, 3).HasColumnName("pieces_per_batch");
            entity.Property(e => e.PreparationTimeMinutes).HasColumnName("preparation_time_minutes");
            entity.Property(e => e.ProductItemId).HasColumnName("product_item_id");
            entity.Property(e => e.RecipeCode).HasMaxLength(50).HasColumnName("recipe_code");
            entity.Property(e => e.RecipeName).HasMaxLength(250).HasColumnName("recipe_name");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP").HasColumnName("updated_at");
            entity.Property(e => e.WastePercentage).HasPrecision(8, 3).HasColumnName("waste_percentage");
            entity.Property(e => e.YieldQuantity).HasPrecision(18, 3).HasColumnName("yield_quantity");
            entity.Property(e => e.YieldUnit).HasMaxLength(50).HasColumnName("yield_unit");

            entity.HasOne(d => d.Company).WithMany(p => p.Recipes)
                .HasForeignKey(d => d.CompanyId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("recipes_company_id_fkey");
        });

        modelBuilder.Entity<RecipeLine>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("recipe_lines_pkey");
            entity.ToTable("recipe_lines");
            entity.HasIndex(e => e.StageId, "idx_recipe_lines_stage");
            entity.HasIndex(e => new { e.RecipeId, e.LineNumber }, "recipe_lines_recipe_id_line_number_key").IsUnique();

            entity.Property(e => e.Id).UseIdentityAlwaysColumn().HasColumnName("id");
            entity.Property(e => e.ComponentItemId).HasColumnName("component_item_id");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP").HasColumnName("created_at");
            entity.Property(e => e.IsOptional).HasColumnName("is_optional");
            entity.Property(e => e.LineNumber).HasColumnName("line_number");
            entity.Property(e => e.Notes).HasColumnName("notes");
            entity.Property(e => e.Quantity).HasPrecision(18, 3).HasColumnName("quantity");
            entity.Property(e => e.RecipeId).HasColumnName("recipe_id");
            entity.Property(e => e.StageId).HasColumnName("stage_id");
            entity.Property(e => e.Unit).HasMaxLength(50).HasColumnName("unit");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP").HasColumnName("updated_at");
            entity.Property(e => e.WastePercentage).HasPrecision(8, 3).HasColumnName("waste_percentage");

            entity.HasOne(d => d.ComponentItem).WithMany()
                .HasForeignKey(d => d.ComponentItemId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("recipe_lines_component_item_id_fkey");

            entity.HasOne(d => d.Recipe).WithMany(p => p.RecipeLines)
                .HasForeignKey(d => d.RecipeId)
                .HasConstraintName("recipe_lines_recipe_id_fkey");

            entity.HasOne(d => d.Stage).WithMany(p => p.RecipeLines)
                .HasForeignKey(d => d.StageId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("recipe_lines_stage_fk");
        });

        modelBuilder.Entity<RecipeStage>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("recipe_stages_pkey");
            entity.ToTable("recipe_stages");
            entity.HasIndex(e => e.OutputItemId, "idx_recipe_stages_output_item");
            entity.HasIndex(e => e.RecipeId, "idx_recipe_stages_recipe");
            entity.HasIndex(e => e.StageRecipeId, "idx_recipe_stages_stage_recipe");
            entity.HasIndex(e => new { e.RecipeId, e.StageNumber }, "recipe_stages_recipe_id_stage_number_key").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Active).HasDefaultValue(true).HasColumnName("active");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP").HasColumnName("created_at");
            entity.Property(e => e.Notes).HasColumnName("notes");
            entity.Property(e => e.OutputItemId).HasColumnName("output_item_id");
            entity.Property(e => e.RecipeId).HasColumnName("recipe_id");
            entity.Property(e => e.StageName).HasMaxLength(200).HasColumnName("stage_name");
            entity.Property(e => e.StageNumber).HasColumnName("stage_number");
            entity.Property(e => e.StageRecipeId).HasColumnName("stage_recipe_id");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP").HasColumnName("updated_at");

            entity.HasOne(d => d.OutputItem).WithMany()
                .HasForeignKey(d => d.OutputItemId)
                .HasConstraintName("recipe_stages_output_item_id_fkey");

            entity.HasOne(d => d.Recipe).WithMany(p => p.RecipeStageRecipes)
                .HasForeignKey(d => d.RecipeId)
                .HasConstraintName("recipe_stages_recipe_id_fkey");

            entity.HasOne(d => d.StageRecipe).WithMany(p => p.RecipeStageStageRecipes)
                .HasForeignKey(d => d.StageRecipeId)
                .HasConstraintName("recipe_stages_stage_recipe_id_fkey");
        });

        // ----------------------------------------------------
        // 14. الأدوار والصلاحيات (Roles)
        // ----------------------------------------------------
        modelBuilder.Entity<AppRole>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("roles_pkey");
            entity.ToTable("roles");
            entity.HasIndex(e => e.CompanyId, "idx_roles_company");
            entity.HasIndex(e => new { e.CompanyId, e.Name }, "roles_company_id_role_code_key").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Active).HasDefaultValue(true).HasColumnName("active");
            entity.Property(e => e.CompanyId).HasColumnName("company_id");
            entity.Property(e => e.Name).HasMaxLength(50).HasColumnName("role_code");
            entity.Property(e => e.DisplayName).HasMaxLength(100).HasColumnName("role_name");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.Permissions).HasColumnName("permissions");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP").HasColumnName("created_at");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP").HasColumnName("updated_at");
        });

        // ----------------------------------------------------
        // 15. أوامر المبيعات (SalesOrders)
        // ----------------------------------------------------
        modelBuilder.Entity<SalesOrder>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("sales_orders_pkey");
            entity.ToTable("sales_orders");
            entity.HasIndex(e => e.CompanyId, "idx_sales_orders_company");
            entity.HasIndex(e => e.CustomerId, "idx_sales_orders_customer");
            entity.HasIndex(e => e.WarehouseId, "idx_sales_orders_warehouse");
            entity.HasIndex(e => new { e.CompanyId, e.OrderNumber }, "sales_orders_company_id_order_number_key").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.BranchId).HasColumnName("branch_id");
            entity.Property(e => e.CompanyId).HasColumnName("company_id");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP").HasColumnName("created_at");
            entity.Property(e => e.CustomerId).HasColumnName("customer_id");
            entity.Property(e => e.DiscountAmount).HasPrecision(18, 2).HasColumnName("discount_amount");
            entity.Property(e => e.Notes).HasColumnName("notes");
            entity.Property(e => e.OrderDate).HasDefaultValueSql("CURRENT_DATE").HasColumnName("order_date");
            entity.Property(e => e.OrderNumber).HasMaxLength(50).HasColumnName("order_number");
            entity.Property(e => e.Status).HasMaxLength(30).HasDefaultValueSql("'DRAFT'").HasColumnName("status");
            entity.Property(e => e.Subtotal).HasPrecision(18, 2).HasColumnName("subtotal");
            entity.Property(e => e.TaxAmount).HasPrecision(18, 2).HasColumnName("tax_amount");
            entity.Property(e => e.TotalAmount).HasPrecision(18, 2).HasColumnName("total_amount");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP").HasColumnName("updated_at");
            entity.Property(e => e.WarehouseId).HasColumnName("warehouse_id");

            entity.HasOne(d => d.Branch).WithMany(p => p.SalesOrders)
                .HasForeignKey(d => d.BranchId)
                .HasConstraintName("sales_orders_branch_fk");

            entity.HasOne(d => d.Company).WithMany(p => p.SalesOrders)
                .HasForeignKey(d => d.CompanyId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("sales_orders_company_fk");

            entity.HasOne(d => d.Customer).WithMany(p => p.SalesOrders)
                .HasForeignKey(d => d.CustomerId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("sales_orders_customer_fk");

            entity.HasOne(d => d.Warehouse).WithMany(p => p.SalesOrders)
                .HasForeignKey(d => d.WarehouseId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("sales_orders_warehouse_fk");
        });

        modelBuilder.Entity<SalesOrderLine>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("sales_order_lines_pkey");
            entity.ToTable("sales_order_lines");
            entity.HasIndex(e => e.ItemId, "idx_sales_lines_item");
            entity.HasIndex(e => e.SalesOrderId, "idx_sales_lines_order");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.DeliveredQuantity).HasPrecision(18, 6).HasColumnName("delivred_quantity");
            entity.Property(e => e.DiscountAmount).HasPrecision(18, 2).HasColumnName("discount_amount");
            entity.Property(e => e.ItemId).HasColumnName("item_id");
            entity.Property(e => e.LineTotal).HasPrecision(18, 2).HasColumnName("line_total");
            entity.Property(e => e.Notes).HasColumnName("notes");
            entity.Property(e => e.Quantity).HasPrecision(18, 6).HasColumnName("quantity");
            entity.Property(e => e.SalesOrderId).HasColumnName("sales_order_id");
            entity.Property(e => e.TaxAmount).HasPrecision(18, 2).HasColumnName("tax_amount");
            entity.Property(e => e.UnitPrice).HasPrecision(18, 2).HasColumnName("unit_price");

            entity.HasOne(d => d.Item).WithMany()
                .HasForeignKey(d => d.ItemId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("sales_lines_item_fk");

            entity.HasOne(d => d.SalesOrder).WithMany(p => p.SalesOrderLines)
                .HasForeignKey(d => d.SalesOrderId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("sales_lines_order_fk");
        });

        // ----------------------------------------------------
        // الأرصدة وحركات المخازن (Stock Movements)
        // ----------------------------------------------------
        modelBuilder.Entity<StockMovement>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("stock_movements_pkey");
            entity.ToTable("stock_movements");
            entity.HasIndex(e => e.CompanyId, "idx_stock_movements_company");
            entity.HasIndex(e => e.MovementAt, "idx_stock_movements_date");
            entity.HasIndex(e => e.ItemId, "idx_stock_movements_item");
            entity.HasIndex(e => new { e.ReferenceType, e.ReferenceId }, "idx_stock_movements_reference");
            entity.HasIndex(e => e.WarehouseId, "idx_stock_movements_warehouse");

            entity.Property(e => e.Id).UseIdentityAlwaysColumn().HasColumnName("id");
            entity.Property(e => e.CompanyId).HasColumnName("company_id");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP").HasColumnName("created_at");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");
            entity.Property(e => e.ItemId).HasColumnName("item_id");
            entity.Property(e => e.MovementAt).HasDefaultValueSql("CURRENT_TIMESTAMP").HasColumnName("movement_at");
            entity.Property(e => e.MovementType).HasMaxLength(30).HasColumnName("movement_type");
            entity.Property(e => e.Notes).HasColumnName("notes");
            entity.Property(e => e.Quantity).HasPrecision(18, 3).HasColumnName("quantity");
            entity.Property(e => e.ReferenceId).HasColumnName("reference_id");
            entity.Property(e => e.ReferenceType).HasMaxLength(50).HasColumnName("reference_type");
            entity.Property(e => e.UnitCost).HasPrecision(18, 4).HasColumnName("unit_cost");
            entity.Property(e => e.WarehouseId).HasColumnName("warehouse_id");

            entity.HasOne(d => d.Company).WithMany(p => p.StockMovements)
                .HasForeignKey(d => d.CompanyId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("stock_movements_company_id_fkey");

            entity.HasOne(d => d.Item).WithMany()
                .HasForeignKey(d => d.ItemId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("stock_movements_item_id_fkey");

            entity.HasOne(d => d.Warehouse).WithMany(p => p.StockMovements)
                .HasForeignKey(d => d.WarehouseId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("stock_movements_warehouse_id_fkey");

            // الربط الصريح الصارم مع عمود created_by لمنع توليد أي اسم وهمي
            entity.HasOne(d => d.CreatedByNavigation)
                .WithMany()
                .HasForeignKey(d => d.CreatedBy)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<StockMovement>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("stock_movements_pkey");
            entity.ToTable("stock_movements");
            entity.HasIndex(e => e.CompanyId, "idx_stock_movements_company");
            entity.HasIndex(e => e.MovementAt, "idx_stock_movements_date");
            entity.HasIndex(e => e.ItemId, "idx_stock_movements_item");
            entity.HasIndex(e => new { e.ReferenceType, e.ReferenceId }, "idx_stock_movements_reference");
            entity.HasIndex(e => e.WarehouseId, "idx_stock_movements_warehouse");

            entity.Property(e => e.Id).UseIdentityAlwaysColumn().HasColumnName("id");
            entity.Property(e => e.CompanyId).HasColumnName("company_id");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP").HasColumnName("created_at");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");
            entity.Property(e => e.ItemId).HasColumnName("item_id");
            entity.Property(e => e.MovementAt).HasDefaultValueSql("CURRENT_TIMESTAMP").HasColumnName("movement_at");
            entity.Property(e => e.MovementType).HasMaxLength(30).HasColumnName("movement_type");
            entity.Property(e => e.Notes).HasColumnName("notes");
            entity.Property(e => e.Quantity).HasPrecision(18, 3).HasColumnName("quantity");
            entity.Property(e => e.ReferenceId).HasColumnName("reference_id");
            entity.Property(e => e.ReferenceType).HasMaxLength(50).HasColumnName("reference_type");
            entity.Property(e => e.UnitCost).HasPrecision(18, 4).HasColumnName("unit_cost");
            entity.Property(e => e.WarehouseId).HasColumnName("warehouse_id");

            entity.HasOne(d => d.Company).WithMany(p => p.StockMovements)
                .HasForeignKey(d => d.CompanyId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("stock_movements_company_id_fkey");

            entity.HasOne(d => d.Item).WithMany()
                .HasForeignKey(d => d.ItemId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("stock_movements_item_id_fkey");

            entity.HasOne(d => d.Warehouse).WithMany(p => p.StockMovements)
                .HasForeignKey(d => d.WarehouseId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("stock_movements_warehouse_id_fkey");
        });

        // ----------------------------------------------------
        // 17. التحويلات المخزنية بين الفروع والمصنع (Transfers)
        // ----------------------------------------------------
        modelBuilder.Entity<StockTransfer>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("stock_transfers_pkey");
            entity.ToTable("stock_transfers");
            entity.HasIndex(e => e.FromWarehouseId, "idx_stock_transfers_from");
            entity.HasIndex(e => e.ToWarehouseId, "idx_stock_transfers_to");
            entity.HasIndex(e => new { e.CompanyId, e.TransferNumber }, "stock_transfers_company_id_transfer_number_key").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CompanyId).HasColumnName("company_id");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP").HasColumnName("created_at");
            entity.Property(e => e.FromWarehouseId).HasColumnName("from_warehouse_id");
            entity.Property(e => e.ToWarehouseId).HasColumnName("to_warehouse_id");
            entity.Property(e => e.TransferDate).HasDefaultValueSql("CURRENT_DATE").HasColumnName("transfer_date");
            entity.Property(e => e.TransferNumber).HasMaxLength(50).HasColumnName("transfer_number");
            entity.Property(e => e.Status).HasMaxLength(30).HasDefaultValueSql("'IN_TRANSIT'").HasColumnName("status");
            entity.Property(e => e.Notes).HasColumnName("notes");
            entity.Property(e => e.DriverName).HasMaxLength(150).HasColumnName("driver_name");
            entity.Property(e => e.VehicleNumber).HasMaxLength(50).HasColumnName("vehicle_number");
            entity.Property(e => e.CreatedBy).HasMaxLength(100).HasColumnName("created_by");
            entity.Property(e => e.ReceivedBy).HasMaxLength(100).HasColumnName("received_by");
            entity.Property(e => e.DispatchedAt).HasColumnName("dispatched_at");
            entity.Property(e => e.CompletedAt).HasColumnName("completed_at");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP").HasColumnName("updated_at");

            entity.HasOne(d => d.Company).WithMany(p => p.StockTransfers)
                .HasForeignKey(d => d.CompanyId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("stock_transfers_company_fk");

            entity.HasOne(d => d.FromWarehouse).WithMany(p => p.StockTransferFromWarehouses)
                .HasForeignKey(d => d.FromWarehouseId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("stock_transfers_from_warehouse_fk");

            entity.HasOne(d => d.ToWarehouse).WithMany(p => p.StockTransferToWarehouses)
                .HasForeignKey(d => d.ToWarehouseId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("stock_transfers_to_warehouse_fk");
        });

        modelBuilder.Entity<StockTransferLine>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("stock_transfer_lines_pkey");
            entity.ToTable("stock_transfer_lines");
            entity.HasIndex(e => e.ItemId, "idx_stock_transfer_lines_item");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.TransferId).HasColumnName("transfer_id");
            entity.Property(e => e.ItemId).HasColumnName("item_id");
            entity.Property(e => e.Quantity).HasPrecision(18, 6).HasColumnName("quantity");
            entity.Property(e => e.ReceivedQuantity).HasPrecision(18, 6).HasColumnName("received_quantity");
            entity.Property(e => e.BatchNumber).HasMaxLength(100).HasColumnName("batch_number");
            entity.Property(e => e.ExpiryDate).HasColumnName("expiry_date");
            entity.Property(e => e.UnitCost).HasPrecision(18, 2).HasColumnName("unit_cost");
            entity.Property(e => e.LineStatus).HasMaxLength(30).HasDefaultValueSql("'PENDING'").HasColumnName("line_status");
            entity.Property(e => e.DiscrepancyReason).HasColumnName("discrepancy_reason");
            entity.Property(e => e.ResolutionNotes).HasColumnName("resolution_notes");
            entity.Property(e => e.LossQty).HasPrecision(18, 3).HasColumnName("loss_qty");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP").HasColumnName("created_at");

            entity.HasOne(d => d.Item).WithMany()
                .HasForeignKey(d => d.ItemId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("stock_transfer_lines_item_fk");

            entity.HasOne(d => d.Transfer).WithMany(p => p.StockTransferLines)
                .HasForeignKey(d => d.TransferId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("stock_transfer_lines_transfer_fk");
        });

        // ----------------------------------------------------
        // 18. الموردين (Suppliers)
        // ----------------------------------------------------
        modelBuilder.Entity<Supplier>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("suppliers_pkey");
            entity.ToTable("suppliers");
            entity.HasIndex(e => e.CompanyId, "ix_suppliers_company_id");
            entity.HasIndex(e => new { e.CompanyId, e.SupplierCode }, "suppliers_company_id_supplier_code_key").IsUnique();

            entity.Property(e => e.Id).UseIdentityAlwaysColumn().HasColumnName("id");
            entity.Property(e => e.Active).HasDefaultValue(true).HasColumnName("active");
            entity.Property(e => e.Address).HasColumnName("address");
            entity.Property(e => e.CompanyId).HasColumnName("company_id");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP").HasColumnName("created_at");
            entity.Property(e => e.CreditLimit).HasPrecision(18, 2).HasColumnName("credit_limit");
            entity.Property(e => e.Email).HasMaxLength(150).HasColumnName("email");
            entity.Property(e => e.Notes).HasColumnName("notes");
            entity.Property(e => e.PaymentTerms).HasMaxLength(100).HasColumnName("payment_terms");
            entity.Property(e => e.Phone).HasMaxLength(50).HasColumnName("phone");
            entity.Property(e => e.SupplierCode).HasMaxLength(50).HasColumnName("supplier_code");
            entity.Property(e => e.SupplierName).HasMaxLength(200).HasColumnName("supplier_name");
            entity.Property(e => e.TaxNumber).HasMaxLength(100).HasColumnName("tax_number");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP").HasColumnName("updated_at");

            entity.HasOne(d => d.Company).WithMany(p => p.Suppliers)
                .HasForeignKey(d => d.CompanyId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("suppliers_company_id_fkey");
        });

        // ----------------------------------------------------
        // 19. وحدات القياس والتحويلات (Units)
        // ----------------------------------------------------
        modelBuilder.Entity<Unit>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("units_pkey");
            entity.ToTable("units");
            entity.HasIndex(e => e.CompanyId, "idx_units_company");
            entity.HasIndex(e => new { e.CompanyId, e.UnitCode }, "units_company_id_unit_code_key").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Active).HasDefaultValue(true).HasColumnName("active");
            entity.Property(e => e.CompanyId).HasColumnName("company_id");
            entity.Property(e => e.UnitCode).HasMaxLength(30).HasColumnName("unit_code");
            entity.Property(e => e.UnitName).HasMaxLength(100).HasColumnName("unit_name");

            entity.HasOne(d => d.Company).WithMany(p => p.Units)
                .HasForeignKey(d => d.CompanyId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("units_company_fk");
        });

        modelBuilder.Entity<UnitConversion>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("unit_conversions_pkey");
            entity.ToTable("unit_conversions");
            entity.HasIndex(e => e.FromUnitId, "idx_unit_conversions_from");
            entity.HasIndex(e => e.ToUnitId, "idx_unit_conversions_to");
            entity.HasIndex(e => new { e.CompanyId, e.FromUnitId, e.ToUnitId }, "unit_conversions_company_id_from_unit_id_to_unit_id_key").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CompanyId).HasColumnName("company_id");
            entity.Property(e => e.ConversionFactor).HasPrecision(18, 6).HasColumnName("conversion_factor");
            entity.Property(e => e.FromUnitId).HasColumnName("from_unit_id");
            entity.Property(e => e.ToUnitId).HasColumnName("to_unit_id");

            entity.HasOne(d => d.Company).WithMany(p => p.UnitConversions)
                .HasForeignKey(d => d.CompanyId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("unit_conversions_company_fk");

            entity.HasOne(d => d.FromUnit).WithMany(p => p.UnitConversionFromUnits)
                .HasForeignKey(d => d.FromUnitId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("unit_conversions_from_unit_fk");

            entity.HasOne(d => d.ToUnit).WithMany(p => p.UnitConversionToUnits)
                .HasForeignKey(d => d.ToUnitId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("unit_conversions_to_unit_fk");
        });

        // ----------------------------------------------------
        // 20. سجلات الهوالك والأصول المتداولة
        // ----------------------------------------------------
        modelBuilder.Entity<WasteRecord>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("waste_records_pkey");
            entity.ToTable("waste_records");
            entity.HasIndex(e => e.ProductionOrderId, "idx_waste_production");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CompanyId).HasColumnName("company_id");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP").HasColumnName("created_at");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");
            entity.Property(e => e.ItemId).HasColumnName("item_id");
            entity.Property(e => e.Notes).HasColumnName("notes");
            entity.Property(e => e.ProductionOrderId).HasColumnName("production_order_id");
            entity.Property(e => e.ProductionStageId).HasColumnName("production_stage_id");
            entity.Property(e => e.Quantity).HasPrecision(18, 6).HasColumnName("quantity");
            entity.Property(e => e.WarehouseId).HasColumnName("warehouse_id");
            entity.Property(e => e.WasteDate).HasDefaultValueSql("CURRENT_TIMESTAMP").HasColumnName("waste_date");
            entity.Property(e => e.WasteReason).HasMaxLength(150).HasColumnName("waste_reason");

            entity.HasOne(d => d.Company).WithMany(p => p.WasteRecords)
                .HasForeignKey(d => d.CompanyId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("waste_company_fk");

            entity.HasOne(d => d.Item).WithMany()
                .HasForeignKey(d => d.ItemId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("waste_item_fk");

            entity.HasOne(d => d.ProductionOrder).WithMany()
                .HasForeignKey(d => d.ProductionOrderId)
                .HasConstraintName("waste_production_order_fk");

            entity.HasOne(d => d.ProductionStage).WithMany()
                .HasForeignKey(d => d.ProductionStageId)
                .HasConstraintName("waste_production_stage_fk");

            entity.HasOne(d => d.Warehouse).WithMany(p => p.WasteRecords)
                .HasForeignKey(d => d.WarehouseId)
                .HasConstraintName("waste_records_warehouse_id_fkey");
        });

        modelBuilder.Entity<AssetBalance>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("asset_balances_pkey");
            entity.ToTable("asset_balances");
            entity.HasIndex(e => new { e.CompanyId, e.ItemId, e.PartyType, e.PartyId }, "ux_asset_balances_unique").IsUnique();

            entity.Property(e => e.Id).UseIdentityAlwaysColumn().HasColumnName("id");
            entity.Property(e => e.CompanyId).HasColumnName("company_id");
            entity.Property(e => e.ItemId).HasColumnName("item_id");
            entity.Property(e => e.PartyType).HasMaxLength(50).HasColumnName("party_type");
            entity.Property(e => e.PartyId).HasColumnName("party_id");
            entity.Property(e => e.Quantity).HasPrecision(18, 3).HasColumnName("quantity");
            entity.Property(e => e.LastUpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP").HasColumnName("last_updated_at");

            entity.HasOne(d => d.Company).WithMany()
                .HasForeignKey(d => d.CompanyId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("asset_balances_company_fk");

            entity.HasOne(d => d.Item).WithMany()
                .HasForeignKey(d => d.ItemId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("asset_balances_item_fk");
        });

        modelBuilder.Entity<AssetMovement>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("asset_movements_pkey");
            entity.ToTable("asset_movements");
            entity.HasIndex(e => new { e.FromPartyType, e.FromPartyId }, "idx_asset_movements_from");
            entity.HasIndex(e => new { e.ToPartyType, e.ToPartyId }, "idx_asset_movements_to");
            entity.HasIndex(e => e.ItemId, "idx_asset_movements_item");

            entity.Property(e => e.Id).UseIdentityAlwaysColumn().HasColumnName("id");
            entity.Property(e => e.CompanyId).HasColumnName("company_id");
            entity.Property(e => e.MovementDate).HasDefaultValueSql("CURRENT_DATE").HasColumnName("movement_date");
            entity.Property(e => e.ItemId).HasColumnName("item_id");
            entity.Property(e => e.FromPartyType).HasMaxLength(50).HasColumnName("from_party_type");
            entity.Property(e => e.FromPartyId).HasColumnName("from_party_id");
            entity.Property(e => e.ToPartyType).HasMaxLength(50).HasColumnName("to_party_type");
            entity.Property(e => e.ToPartyId).HasColumnName("to_party_id");
            entity.Property(e => e.Quantity).HasPrecision(18, 3).HasColumnName("quantity");
            entity.Property(e => e.MovementType).HasMaxLength(30).HasColumnName("movement_type");
            entity.Property(e => e.Notes).HasColumnName("notes");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP").HasColumnName("created_at");

            entity.HasOne(d => d.Company).WithMany()
                .HasForeignKey(d => d.CompanyId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("asset_movements_company_fk");

            entity.HasOne(d => d.Item).WithMany()
                .HasForeignKey(d => d.ItemId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("asset_movements_item_fk");
        });

        // ----------------------------------------------------
        // 21. الأصول الثابتة والأسبات
        // ----------------------------------------------------
        modelBuilder.Entity<FixedAsset>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("fixed_assets_pkey");
            entity.ToTable("fixed_assets");
            entity.HasIndex(e => e.AssetCode, "idx_fixed_assets_asset_code").IsUnique();

            entity.Property(e => e.Id).UseIdentityAlwaysColumn().HasColumnName("id");
            entity.Property(e => e.AssetCode).HasMaxLength(50).HasColumnName("asset_code");
            entity.Property(e => e.Name).HasMaxLength(200).HasColumnName("name");
            entity.Property(e => e.Department).HasMaxLength(150).HasColumnName("department");
            entity.Property(e => e.Category).HasMaxLength(100).HasColumnName("category");
            entity.Property(e => e.BrandModel).HasMaxLength(150).HasColumnName("brand_model");
            entity.Property(e => e.SerialNumber).HasMaxLength(100).HasColumnName("serial_number");
            entity.Property(e => e.CustodianCode).HasMaxLength(50).HasColumnName("custodian_code");
            entity.Property(e => e.CustodianName).HasMaxLength(150).HasColumnName("custodian_name");
            entity.Property(e => e.InstallationDate).HasColumnName("installation_date");
            entity.Property(e => e.Status).HasMaxLength(50).HasColumnName("status");
            entity.Property(e => e.ActiveSOSTaskNumber).HasMaxLength(50).HasColumnName("active_sos_task_number");

            entity.HasMany(e => e.HistoryLogs)
                  .WithOne(l => l.FixedAsset)
                  .HasForeignKey(l => l.FixedAssetId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<AssetHistoryLog>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("asset_history_logs_pkey");
            entity.ToTable("asset_history_logs");

            entity.Property(e => e.Id).UseIdentityAlwaysColumn().HasColumnName("id");
            entity.Property(e => e.FixedAssetId).HasColumnName("fixed_asset_id");
            entity.Property(e => e.Date).HasColumnName("date");
            entity.Property(e => e.EventType).HasMaxLength(100).HasColumnName("event_type");
            entity.Property(e => e.Description).HasMaxLength(500).HasColumnName("description");
            entity.Property(e => e.PerformedByCode).HasMaxLength(50).HasColumnName("performed_by_code");
            entity.Property(e => e.PerformedByName).HasMaxLength(150).HasColumnName("performed_by_name");
            entity.Property(e => e.CostImpact).HasMaxLength(100).HasColumnName("cost_impact");
        });

        modelBuilder.Entity<CirculatingContainer>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("circulating_containers_pkey");
            entity.ToTable("circulating_containers");
            entity.HasIndex(e => e.ContainerName, "idx_circulating_containers_name").IsUnique();

            entity.Property(e => e.Id).UseIdentityAlwaysColumn().HasColumnName("id");
            entity.Property(e => e.ContainerName).HasMaxLength(150).HasColumnName("container_name");
            entity.Property(e => e.Specification).HasMaxLength(200).HasColumnName("specification");
            entity.Property(e => e.TotalFactoryQuantity).HasColumnName("total_factory_quantity");
            entity.Property(e => e.InProductionFloorQuantity).HasColumnName("in_production_floor_quantity");
            entity.Property(e => e.InPackagingWarehouseQuantity).HasColumnName("in_packaging_warehouse_quantity");
            entity.Property(e => e.WithDriversAndBranchesQuantity).HasColumnName("with_drivers_and_branches_quantity");
            entity.Property(e => e.DisputedQuantity).HasColumnName("disputed_quantity");
            entity.Property(e => e.DamagedQuantity).HasColumnName("damaged_quantity");
        });

        modelBuilder.Entity<CrateMovement>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("crate_movements_pkey");
            entity.ToTable("crate_movements");
            entity.HasIndex(e => e.MovementNumber, "idx_crate_movements_number").IsUnique();

            entity.Property(e => e.Id).UseIdentityAlwaysColumn().HasColumnName("id");
            entity.Property(e => e.MovementNumber).HasMaxLength(50).HasColumnName("movement_number");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.MovementType).HasMaxLength(50).HasColumnName("movement_type");
            entity.Property(e => e.ContainerName).HasMaxLength(150).HasColumnName("container_name");
            entity.Property(e => e.DispatchedQuantity).HasColumnName("dispatched_quantity");
            entity.Property(e => e.SourceLocation).HasMaxLength(150).HasColumnName("source_location");
            entity.Property(e => e.DispatchedByCode).HasMaxLength(50).HasColumnName("dispatched_by_code");
            entity.Property(e => e.DispatchedByName).HasMaxLength(150).HasColumnName("dispatched_by_name");
            entity.Property(e => e.DestinationLocation).HasMaxLength(150).HasColumnName("destination_location");
            entity.Property(e => e.Notes).HasMaxLength(500).HasColumnName("notes");
            entity.Property(e => e.ReceivedQuantity).HasColumnName("received_quantity");
            entity.Property(e => e.ShortageQuantity).HasColumnName("shortage_quantity");
            entity.Property(e => e.DamagedCount).HasColumnName("damaged_count");
            entity.Property(e => e.ReceivedAt).HasColumnName("received_at");
            entity.Property(e => e.ReceivedByCode).HasMaxLength(50).HasColumnName("received_by_code");
            entity.Property(e => e.ReceivedByName).HasMaxLength(150).HasColumnName("received_by_name");
            entity.Property(e => e.ReceiveNotes).HasMaxLength(500).HasColumnName("receive_notes");
            entity.Property(e => e.Status).HasMaxLength(50).HasColumnName("status");
            entity.Property(e => e.ArbitrationDecision).HasMaxLength(100).HasColumnName("arbitration_decision");
            entity.Property(e => e.ArbitrationText).HasMaxLength(500).HasColumnName("arbitration_text");
            entity.Property(e => e.ArbitratedByCode).HasMaxLength(50).HasColumnName("arbitrated_by_code");
            entity.Property(e => e.ArbitratedByName).HasMaxLength(150).HasColumnName("arbitrated_by_name");
            entity.Property(e => e.FinancialPenalty).HasMaxLength(100).HasColumnName("financial_penalty");
        });

        // ----------------------------------------------------
        // 22. النماذج الديناميكية وبلاغات الطوارئ
        // ----------------------------------------------------
        modelBuilder.Entity<DynamicForm>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("dynamic_forms_pkey");
            entity.ToTable("dynamic_forms");
            entity.HasIndex(e => e.FormCode, "dynamic_forms_form_code_key").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP").HasColumnName("created_at");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.FieldsSchema).HasColumnType("text").HasColumnName("fields_schema");
            entity.Property(e => e.FormCode).HasMaxLength(100).HasColumnName("form_code");
            entity.Property(e => e.FormName).HasMaxLength(255).HasColumnName("form_name");
            entity.Property(e => e.IsActive).HasDefaultValue(true).HasColumnName("is_active");
        });

        modelBuilder.Entity<FormSubmission>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("form_submissions_pkey");
            entity.ToTable("form_submissions");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.FormId).HasColumnName("form_id");
            entity.Property(e => e.Status).HasMaxLength(50).HasDefaultValueSql("'Submitted'").HasColumnName("status");
            entity.Property(e => e.SubmissionData).HasColumnType("text").HasColumnName("submission_data");
            entity.Property(e => e.SubmittedAt).HasDefaultValueSql("CURRENT_TIMESTAMP").HasColumnName("submitted_at");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.Form).WithMany(p => p.FormSubmissions)
                .HasForeignKey(d => d.FormId)
                .HasConstraintName("form_submissions_form_id_fkey");
        });

        modelBuilder.Entity<EmergencyTask>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("emergency_tasks_pkey");
            entity.ToTable("emergency_tasks");
            entity.HasIndex(e => e.TaskNumber, "idx_emergency_tasks_number").IsUnique();
            entity.HasIndex(e => e.Status, "idx_emergency_tasks_status");

            entity.Property(e => e.Id).UseIdentityAlwaysColumn().HasColumnName("id");
            entity.Property(e => e.TaskNumber).HasMaxLength(50).HasColumnName("task_number");
            entity.Property(e => e.Title).HasMaxLength(250).HasColumnName("title");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.SenderDepartment).HasMaxLength(150).HasColumnName("sender_department");
            entity.Property(e => e.SenderEmployeeCode).HasMaxLength(50).HasColumnName("sender_employee_code");
            entity.Property(e => e.SenderEmployeeName).HasMaxLength(150).HasColumnName("sender_employee_name");
            entity.Property(e => e.TargetDepartment).HasMaxLength(150).HasColumnName("target_department");
            entity.Property(e => e.TargetEmployeeName).HasMaxLength(150).HasColumnName("target_employee_name");
            entity.Property(e => e.AssignedTechnicianCode).HasMaxLength(50).HasColumnName("assigned_technician_code");
            entity.Property(e => e.AssignedTechnicianName).HasMaxLength(150).HasColumnName("assigned_technician_name");
            entity.Property(e => e.AssignmentNotes).HasColumnName("assignment_notes");
            entity.Property(e => e.Priority).HasMaxLength(30).HasDefaultValue("NORMAL").HasColumnName("priority");
            entity.Property(e => e.MediaType).HasMaxLength(30).HasDefaultValue("TEXT").HasColumnName("media_type");
            entity.Property(e => e.MediaUrl).HasColumnName("media_url");
            entity.Property(e => e.MediaFileName).HasMaxLength(200).HasColumnName("media_file_name");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP").HasColumnName("created_at");
            entity.Property(e => e.TargetResolutionMinutes).HasDefaultValue(30).HasColumnName("target_resolution_minutes");
            entity.Property(e => e.RespondedAt).HasColumnName("responded_at");
            entity.Property(e => e.CompletedAt).HasColumnName("completed_at");
            entity.Property(e => e.Status).HasMaxLength(50).HasDefaultValue("PENDING").HasColumnName("status");
            entity.Property(e => e.ClosureNotes).HasColumnName("closure_notes");
            entity.Property(e => e.ClosureProofMediaUrl).HasColumnName("closure_proof_media_url");
            entity.Property(e => e.ClosedBy).HasMaxLength(150).HasColumnName("closed_by");
            entity.Property(e => e.ClosedByCode).HasMaxLength(50).HasColumnName("closed_by_code");
            entity.Property(e => e.AssetCode).HasMaxLength(50).HasColumnName("asset_code");
            entity.Property(e => e.CostImpact).HasMaxLength(150).HasColumnName("cost_impact");
            entity.Property(e => e.SparePartsUsed).HasColumnName("spare_parts_used");
            entity.Property(e => e.RootCause).HasColumnName("root_cause");
            entity.Property(e => e.DowntimeMinutes).HasDefaultValue(0).HasColumnName("downtime_minutes");
        });

        // ----------------------------------------------------
        // 23. الصيانة الدورية للأصول (Maintenance)
        // ----------------------------------------------------
        modelBuilder.Entity<MaintenanceAsset>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("maintenance_assets_pkey");
            entity.ToTable("maintenance_assets");
            entity.HasIndex(e => new { e.CompanyId, e.AssetCode }, "maintenance_assets_company_id_asset_code_key").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Active).HasDefaultValue(true).HasColumnName("active");
            entity.Property(e => e.AssetCode).HasMaxLength(50).HasColumnName("asset_code");
            entity.Property(e => e.AssetName).HasMaxLength(150).HasColumnName("asset_name");
            entity.Property(e => e.AssetType).HasMaxLength(100).HasColumnName("asset_type");
            entity.Property(e => e.CompanyId).HasColumnName("company_id");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP").HasColumnName("created_at");
            entity.Property(e => e.DepartmentId).HasColumnName("department_id");
            entity.Property(e => e.Location).HasMaxLength(150).HasColumnName("location");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP").HasColumnName("updated_at");

            entity.HasOne(d => d.Company).WithMany(p => p.MaintenanceAssets)
                .HasForeignKey(d => d.CompanyId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("maintenance_assets_company_fk");

            entity.HasOne(d => d.Department).WithMany(p => p.MaintenanceAssets)
                .HasForeignKey(d => d.DepartmentId)
                .HasConstraintName("maintenance_assets_department_fk");
        });

        modelBuilder.Entity<MaintenanceOrder>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("maintenance_orders_pkey");
            entity.ToTable("maintenance_orders");
            entity.HasIndex(e => e.AssetId, "idx_maintenance_asset");
            entity.HasIndex(e => new { e.CompanyId, e.OrderNumber }, "maintenance_orders_company_id_order_number_key").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.AssetId).HasColumnName("asset_id");
            entity.Property(e => e.CompanyId).HasColumnName("company_id");
            entity.Property(e => e.CompletedDate).HasColumnName("completed_date");
            entity.Property(e => e.Cost).HasPrecision(18, 2).HasColumnName("cost");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP").HasColumnName("created_at");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.MaintenanceType).HasMaxLength(30).HasDefaultValueSql("'CORRECTIVE'").HasColumnName("maintenance_type");
            entity.Property(e => e.Notes).HasColumnName("notes");
            entity.Property(e => e.OrderNumber).HasMaxLength(50).HasColumnName("order_number");
            entity.Property(e => e.Priority).HasMaxLength(20).HasDefaultValueSql("'NORMAL'").HasColumnName("priority");
            entity.Property(e => e.ScheduledDate).HasColumnName("scheduled_date");
            entity.Property(e => e.Status).HasMaxLength(30).HasDefaultValueSql("'OPEN'").HasColumnName("status");
            entity.Property(e => e.Technician).HasMaxLength(150).HasColumnName("technician");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP").HasColumnName("updated_at");

            entity.HasOne(d => d.Asset).WithMany(p => p.MaintenanceOrders)
                .HasForeignKey(d => d.AssetId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("maintenance_orders_asset_fk");

            entity.HasOne(d => d.Company).WithMany(p => p.MaintenanceOrders)
                .HasForeignKey(d => d.CompanyId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("maintenance_orders_company_fk");
        });

        // ----------------------------------------------------
        // 24. عروض أسعار الموردين والViews المرجعية
        // ----------------------------------------------------
        modelBuilder.Entity<VCoreItem>(entity =>
        {
            entity.HasNoKey().ToView("v_core_items");
            entity.Property(e => e.Active).HasColumnName("active");
            entity.Property(e => e.Barcode).HasMaxLength(100).HasColumnName("barcode");
            entity.Property(e => e.CategoryCode).HasMaxLength(50).HasColumnName("category_code");
            entity.Property(e => e.CategoryId).HasColumnName("category_id");
            entity.Property(e => e.CategoryName).HasMaxLength(200).HasColumnName("category_name");
            entity.Property(e => e.CompanyId).HasColumnName("company_id");
            entity.Property(e => e.CostPrice).HasPrecision(18, 2).HasColumnName("cost_price");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.ItemCode).HasMaxLength(100).HasColumnName("item_code");
            entity.Property(e => e.ItemName).HasMaxLength(250).HasColumnName("item_name");
            entity.Property(e => e.ItemType).HasMaxLength(30).HasColumnName("item_type");
            entity.Property(e => e.MaximumStock).HasPrecision(18, 3).HasColumnName("maximum_stock");
            entity.Property(e => e.MinimumStock).HasPrecision(18, 3).HasColumnName("minimum_stock");
            entity.Property(e => e.PurchasePrice).HasPrecision(18, 2).HasColumnName("purchase_price");
            entity.Property(e => e.SellingPrice).HasPrecision(18, 2).HasColumnName("selling_price");
            entity.Property(e => e.Unit).HasMaxLength(50).HasColumnName("unit");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
        });

        modelBuilder.Entity<VMaterialShortage>(entity =>
        {
            entity.HasNoKey().ToView("v_material_shortages");
            entity.Property(e => e.CompanyId).HasColumnName("company_id");
            entity.Property(e => e.CurrentStock).HasColumnName("current_stock");
            entity.Property(e => e.ItemCode).HasMaxLength(100).HasColumnName("item_code");
            entity.Property(e => e.ItemId).HasColumnName("item_id");
            entity.Property(e => e.ItemName).HasMaxLength(250).HasColumnName("item_name");
            entity.Property(e => e.MinimumStock).HasPrecision(18, 3).HasColumnName("minimum_stock");
            entity.Property(e => e.RequiredPurchasing).HasColumnName("required_purchasing");
        });

        modelBuilder.Entity<VMultiLevelRecipe>(entity =>
        {
            entity.HasNoKey().ToView("v_multi_level_recipes");
            entity.Property(e => e.Active).HasColumnName("active");
            entity.Property(e => e.BatchQuantity).HasPrecision(18, 3).HasColumnName("batch_quantity");
            entity.Property(e => e.BatchUnit).HasMaxLength(50).HasColumnName("batch_unit");
            entity.Property(e => e.CompanyId).HasColumnName("company_id");
            entity.Property(e => e.ComponentCount).HasColumnName("component_count");
            entity.Property(e => e.EffectiveFrom).HasColumnName("effective_from");
            entity.Property(e => e.EffectiveTo).HasColumnName("effective_to");
            entity.Property(e => e.IntermediateComponentCount).HasColumnName("intermediate_component_count");
            entity.Property(e => e.PiecesPerBatch).HasPrecision(18, 3).HasColumnName("pieces_per_batch");
            entity.Property(e => e.ProductItemId).HasColumnName("product_item_id");
            entity.Property(e => e.ProductName).HasMaxLength(250).HasColumnName("product_name");
            entity.Property(e => e.ProductType).HasMaxLength(30).HasColumnName("product_type");
            entity.Property(e => e.RawMaterialComponentCount).HasColumnName("raw_material_component_count");
            entity.Property(e => e.RecipeCode).HasMaxLength(50).HasColumnName("recipe_code");
            entity.Property(e => e.RecipeId).HasColumnName("recipe_id");
            entity.Property(e => e.RecipeName).HasMaxLength(250).HasColumnName("recipe_name");
            entity.Property(e => e.WastePercentage).HasPrecision(8, 3).HasColumnName("waste_percentage");
            entity.Property(e => e.YieldQuantity).HasPrecision(18, 3).HasColumnName("yield_quantity");
            entity.Property(e => e.YieldUnit).HasMaxLength(50).HasColumnName("yield_unit");
        });

        modelBuilder.Entity<VProductionOrderDetail>(entity =>
        {
            entity.HasNoKey().ToView("v_production_order_details");
            entity.Property(e => e.CompanyId).HasColumnName("company_id");
            entity.Property(e => e.ConsumedQuantity).HasPrecision(14, 3).HasColumnName("consumed_quantity");
            entity.Property(e => e.MaterialItemId).HasColumnName("material_item_id");
            entity.Property(e => e.MaterialName).HasMaxLength(250).HasColumnName("material_name");
            entity.Property(e => e.OrderDate).HasColumnName("order_date");
            entity.Property(e => e.OrderNumber).HasMaxLength(50).HasColumnName("order_number");
            entity.Property(e => e.OrderStatus).HasMaxLength(30).HasColumnName("order_status");
            entity.Property(e => e.PlannedPieces).HasPrecision(14, 3).HasColumnName("planned_pieces");
            entity.Property(e => e.PlannedQuantity).HasPrecision(14, 3).HasColumnName("planned_quantity");
            entity.Property(e => e.ProducedPieces).HasPrecision(14, 3).HasColumnName("produced_pieces");
            entity.Property(e => e.ProducedQuantity).HasPrecision(14, 3).HasColumnName("produced_quantity");
            entity.Property(e => e.ProductItemId).HasColumnName("product_item_id");
            entity.Property(e => e.ProductName).HasMaxLength(250).HasColumnName("product_name");
            entity.Property(e => e.ProductionOrderId).HasColumnName("production_order_id");
            entity.Property(e => e.ProductionStageId).HasColumnName("production_stage_id");
            entity.Property(e => e.RecipeCode).HasMaxLength(50).HasColumnName("recipe_code");
            entity.Property(e => e.RecipeId).HasColumnName("recipe_id");
            entity.Property(e => e.RecipeName).HasMaxLength(250).HasColumnName("recipe_name");
            entity.Property(e => e.RequiredQuantity).HasPrecision(14, 3).HasColumnName("required_quantity");
            entity.Property(e => e.StageItemId).HasColumnName("stage_item_id");
            entity.Property(e => e.StageItemName).HasMaxLength(250).HasColumnName("stage_item_name");
            entity.Property(e => e.StageNumber).HasColumnName("stage_number");
            entity.Property(e => e.StagePlannedQuantity).HasPrecision(14, 3).HasColumnName("stage_planned_quantity");
            entity.Property(e => e.StageProducedQuantity).HasPrecision(14, 3).HasColumnName("stage_produced_quantity");
            entity.Property(e => e.StageRecipeId).HasColumnName("stage_recipe_id");
            entity.Property(e => e.StageRecipeName).HasMaxLength(250).HasColumnName("stage_recipe_name");
            entity.Property(e => e.StageStatus).HasMaxLength(30).HasColumnName("stage_status");
            entity.Property(e => e.Unit).HasMaxLength(50).HasColumnName("unit");
            entity.Property(e => e.WarehouseId).HasColumnName("warehouse_id");
            entity.Property(e => e.WastePercentage).HasPrecision(8, 3).HasColumnName("waste_percentage");
        });

        modelBuilder.HasSequence("production_order_number_seq");

        // ⚠️ ده السطر الجديد — يطبّق الفلاتر تلقائيًا
        ApplyTenantQueryFilters(modelBuilder);

        OnModelCreatingPartial(modelBuilder);
    }

    /// <summary>
    /// يضيف Global Query Filter لكل Entity بيطبق ITenantEntity.
    /// كل استعلام على الجداول دي هيرجع بس بيانات الشركة الحالية.
    /// </summary>
    private void ApplyTenantQueryFilters(ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            // هل الـ entity بيطبق ITenantEntity؟
            if (!typeof(ITenantEntity).IsAssignableFrom(entityType.ClrType))
                continue;

            // نبني الـ Filter Expression: e => _tenantContext == null || e.CompanyId == _tenantContext.CompanyId
            var parameter = Expression.Parameter(entityType.ClrType, "e");
            var companyIdProperty = Expression.Property(parameter, nameof(ITenantEntity.CompanyId));

            // الـ TenantContext
            var tenantContextField = Expression.Field(
                Expression.Constant(this),
                "_tenantContext");

            // _tenantContext == null
            var tenantNullCheck = Expression.Equal(
                tenantContextField,
                Expression.Constant(null, typeof(ITenantContext)));

            // _tenantContext.CompanyId
            var tenantCompanyIdProperty = Expression.Property(
                tenantContextField,
                nameof(ITenantContext.CompanyId));

            // e.CompanyId == _tenantContext.CompanyId
            var companyIdComparison = Expression.Equal(
                companyIdProperty,
                Expression.Convert(tenantCompanyIdProperty, typeof(long)));

            // الـ Expression النهائي:
            // _tenantContext == null OR e.CompanyId == _tenantContext.CompanyId
            var body = Expression.OrElse(
                tenantNullCheck,
                companyIdComparison);

            var lambda = Expression.Lambda(body, parameter);

            modelBuilder.Entity(entityType.ClrType)
                .HasQueryFilter(lambda);
        }
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}