using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace KayanERP.Api.Migrations
{
    /// <inheritdoc />
    public partial class InitialPostgresSetup : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "erp");

            migrationBuilder.CreateSequence(
                name: "production_order_number_seq",
                schema: "erp");

            migrationBuilder.CreateTable(
                name: "accounts",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    company_id = table.Column<long>(type: "bigint", nullable: false),
                    account_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    account_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    account_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    parent_account_id = table.Column<long>(type: "bigint", nullable: true),
                    active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_accounts", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "companies",
                schema: "erp",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    company_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    company_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    phone = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    email = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    address = table.Column<string>(type: "text", nullable: true),
                    active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("companies_pkey", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "CrateClients",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    CrateBalance = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CrateClients", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "crates",
                schema: "erp",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    crate_name = table.Column<string>(type: "text", nullable: false),
                    total_factory_balance = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_crates", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "dynamic_forms",
                schema: "erp",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    form_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    form_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    fields_schema = table.Column<string>(type: "text", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: true, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("dynamic_forms_pkey", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "journal_entries",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    company_id = table.Column<long>(type: "bigint", nullable: false),
                    entry_number = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    entry_date = table.Column<DateOnly>(type: "date", nullable: false),
                    reference_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    reference_id = table.Column<long>(type: "bigint", nullable: true),
                    notes = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_journal_entries", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "purchase_invoices",
                schema: "erp",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    invoice_number = table.Column<string>(type: "text", nullable: false),
                    invoice_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    supplier_id = table.Column<long>(type: "bigint", nullable: false),
                    total_amount = table.Column<decimal>(type: "numeric", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_purchase_invoices", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "sales_invoices",
                schema: "erp",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    invoice_number = table.Column<string>(type: "text", nullable: false),
                    invoice_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    customer_id = table.Column<long>(type: "bigint", nullable: false),
                    total_amount = table.Column<decimal>(type: "numeric", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sales_invoices", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "branches",
                schema: "erp",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    company_id = table.Column<long>(type: "bigint", nullable: false),
                    branch_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    branch_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    phone = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    address = table.Column<string>(type: "text", nullable: true),
                    active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("branches_pkey", x => x.id);
                    table.ForeignKey(
                        name: "branches_company_id_fkey",
                        column: x => x.company_id,
                        principalSchema: "erp",
                        principalTable: "companies",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "customers",
                schema: "erp",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    company_id = table.Column<long>(type: "bigint", nullable: false),
                    customer_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    customer_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    customer_type = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false, defaultValueSql: "'RETAIL'"),
                    phone = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    email = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    address = table.Column<string>(type: "text", nullable: true),
                    tax_number = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    payment_terms = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    credit_limit = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    notes = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("customers_pkey", x => x.id);
                    table.ForeignKey(
                        name: "customers_company_id_fkey",
                        column: x => x.company_id,
                        principalSchema: "erp",
                        principalTable: "companies",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "departments",
                schema: "erp",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    company_id = table.Column<long>(type: "bigint", nullable: false),
                    parent_department_id = table.Column<long>(type: "bigint", nullable: true),
                    department_name = table.Column<string>(type: "text", nullable: false),
                    department_code = table.Column<string>(type: "text", nullable: false),
                    description = table.Column<string>(type: "text", nullable: false),
                    active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("departments_pkey", x => x.id);
                    table.ForeignKey(
                        name: "departments_company_id_fkey",
                        column: x => x.company_id,
                        principalSchema: "erp",
                        principalTable: "companies",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "departments_parent_id_fkey",
                        column: x => x.parent_department_id,
                        principalSchema: "erp",
                        principalTable: "departments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "item_categories",
                schema: "erp",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    company_id = table.Column<long>(type: "bigint", nullable: false),
                    category_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    category_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    item_type = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("item_categories_pkey", x => x.id);
                    table.ForeignKey(
                        name: "item_categories_company_id_fkey",
                        column: x => x.company_id,
                        principalSchema: "erp",
                        principalTable: "companies",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "job_titles",
                schema: "erp",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    company_id = table.Column<long>(type: "bigint", nullable: false),
                    title_name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    job_grade = table.Column<int>(type: "integer", nullable: false),
                    active = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("job_titles_pkey", x => x.id);
                    table.ForeignKey(
                        name: "FK_job_titles_companies_company_id",
                        column: x => x.company_id,
                        principalSchema: "erp",
                        principalTable: "companies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "roles",
                schema: "erp",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    company_id = table.Column<long>(type: "bigint", nullable: false),
                    role_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    role_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("roles_pkey", x => x.id);
                    table.ForeignKey(
                        name: "roles_company_fk",
                        column: x => x.company_id,
                        principalSchema: "erp",
                        principalTable: "companies",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "suppliers",
                schema: "erp",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    company_id = table.Column<long>(type: "bigint", nullable: false),
                    supplier_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    supplier_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    phone = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    email = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    address = table.Column<string>(type: "text", nullable: true),
                    tax_number = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    payment_terms = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    credit_limit = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    notes = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("suppliers_pkey", x => x.id);
                    table.ForeignKey(
                        name: "suppliers_company_id_fkey",
                        column: x => x.company_id,
                        principalSchema: "erp",
                        principalTable: "companies",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "units",
                schema: "erp",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    company_id = table.Column<long>(type: "bigint", nullable: false),
                    unit_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    unit_code = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("units_pkey", x => x.id);
                    table.ForeignKey(
                        name: "units_company_fk",
                        column: x => x.company_id,
                        principalSchema: "erp",
                        principalTable: "companies",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "CrateClientTransactions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CrateClientId = table.Column<int>(type: "integer", nullable: false),
                    TransactionType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
                    TransactionDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Notes = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CrateClientTransactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CrateClientTransactions_CrateClients_CrateClientId",
                        column: x => x.CrateClientId,
                        principalTable: "CrateClients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "crate_transactions",
                schema: "erp",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    crate_id = table.Column<long>(type: "bigint", nullable: false),
                    transaction_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    transaction_type = table.Column<string>(type: "text", nullable: false),
                    party_type = table.Column<string>(type: "text", nullable: false),
                    party_id = table.Column<long>(type: "bigint", nullable: false),
                    quantity = table.Column<int>(type: "integer", nullable: false),
                    notes = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_crate_transactions", x => x.id);
                    table.ForeignKey(
                        name: "FK_crate_transactions_crates_crate_id",
                        column: x => x.crate_id,
                        principalSchema: "erp",
                        principalTable: "crates",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "form_submissions",
                schema: "erp",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    form_id = table.Column<long>(type: "bigint", nullable: false),
                    user_id = table.Column<long>(type: "bigint", nullable: true),
                    submission_data = table.Column<string>(type: "text", nullable: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true, defaultValueSql: "'Submitted'"),
                    submitted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("form_submissions_pkey", x => x.id);
                    table.ForeignKey(
                        name: "form_submissions_form_id_fkey",
                        column: x => x.form_id,
                        principalSchema: "erp",
                        principalTable: "dynamic_forms",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "journal_entry_lines",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    journal_entry_id = table.Column<long>(type: "bigint", nullable: false),
                    account_id = table.Column<long>(type: "bigint", nullable: false),
                    debit = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    credit = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_journal_entry_lines", x => x.id);
                    table.ForeignKey(
                        name: "FK_journal_entry_lines_accounts_account_id",
                        column: x => x.account_id,
                        principalTable: "accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_journal_entry_lines_journal_entries_journal_entry_id",
                        column: x => x.journal_entry_id,
                        principalTable: "journal_entries",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "purchase_invoice_lines",
                schema: "erp",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    purchase_invoice_id = table.Column<long>(type: "bigint", nullable: false),
                    item_id = table.Column<long>(type: "bigint", nullable: false),
                    quantity = table.Column<decimal>(type: "numeric", nullable: false),
                    unit_price = table.Column<decimal>(type: "numeric", nullable: false),
                    line_total = table.Column<decimal>(type: "numeric", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_purchase_invoice_lines", x => x.id);
                    table.ForeignKey(
                        name: "FK_purchase_invoice_lines_purchase_invoices_purchase_invoice_id",
                        column: x => x.purchase_invoice_id,
                        principalSchema: "erp",
                        principalTable: "purchase_invoices",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "sales_invoice_lines",
                schema: "erp",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    sales_invoice_id = table.Column<long>(type: "bigint", nullable: false),
                    item_id = table.Column<long>(type: "bigint", nullable: false),
                    quantity = table.Column<int>(type: "integer", nullable: false),
                    unit_price = table.Column<decimal>(type: "numeric", nullable: false),
                    line_total = table.Column<decimal>(type: "numeric", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sales_invoice_lines", x => x.id);
                    table.ForeignKey(
                        name: "FK_sales_invoice_lines_sales_invoices_sales_invoice_id",
                        column: x => x.sales_invoice_id,
                        principalSchema: "erp",
                        principalTable: "sales_invoices",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "warehouses",
                schema: "erp",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    company_id = table.Column<long>(type: "bigint", nullable: false),
                    branch_id = table.Column<long>(type: "bigint", nullable: true),
                    warehouse_name = table.Column<string>(type: "text", nullable: false),
                    warehouse_code = table.Column<string>(type: "text", nullable: false),
                    warehouse_type = table.Column<string>(type: "text", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_warehouses", x => x.id);
                    table.ForeignKey(
                        name: "FK_warehouses_branches_branch_id",
                        column: x => x.branch_id,
                        principalSchema: "erp",
                        principalTable: "branches",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_warehouses_companies_company_id",
                        column: x => x.company_id,
                        principalSchema: "erp",
                        principalTable: "companies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "maintenance_assets",
                schema: "erp",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    company_id = table.Column<long>(type: "bigint", nullable: false),
                    department_id = table.Column<long>(type: "bigint", nullable: true),
                    asset_name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    asset_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    asset_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    location = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("maintenance_assets_pkey", x => x.id);
                    table.ForeignKey(
                        name: "maintenance_assets_company_fk",
                        column: x => x.company_id,
                        principalSchema: "erp",
                        principalTable: "companies",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "maintenance_assets_department_fk",
                        column: x => x.department_id,
                        principalSchema: "erp",
                        principalTable: "departments",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "items",
                schema: "erp",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    company_id = table.Column<long>(type: "bigint", nullable: false),
                    item_code = table.Column<string>(type: "text", nullable: false),
                    item_name = table.Column<string>(type: "text", nullable: false),
                    item_type = table.Column<string>(type: "text", nullable: false),
                    category_id = table.Column<long>(type: "bigint", nullable: false),
                    unit = table.Column<string>(type: "text", nullable: false),
                    active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_items", x => x.id);
                    table.ForeignKey(
                        name: "FK_items_companies_company_id",
                        column: x => x.company_id,
                        principalSchema: "erp",
                        principalTable: "companies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_items_item_categories_category_id",
                        column: x => x.category_id,
                        principalSchema: "erp",
                        principalTable: "item_categories",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "employees",
                schema: "erp",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    company_id = table.Column<long>(type: "bigint", nullable: false),
                    branch_id = table.Column<long>(type: "bigint", nullable: true),
                    department_id = table.Column<long>(type: "bigint", nullable: true),
                    job_title_id = table.Column<long>(type: "bigint", nullable: true),
                    manager_id = table.Column<long>(type: "bigint", nullable: true),
                    employee_name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    employee_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("employees_pkey", x => x.id);
                    table.ForeignKey(
                        name: "FK_employees_branches_branch_id",
                        column: x => x.branch_id,
                        principalSchema: "erp",
                        principalTable: "branches",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_employees_companies_company_id",
                        column: x => x.company_id,
                        principalSchema: "erp",
                        principalTable: "companies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_employees_job_titles_job_title_id",
                        column: x => x.job_title_id,
                        principalSchema: "erp",
                        principalTable: "job_titles",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "employees_department_id_fkey",
                        column: x => x.department_id,
                        principalSchema: "erp",
                        principalTable: "departments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "employees_manager_id_fkey",
                        column: x => x.manager_id,
                        principalSchema: "erp",
                        principalTable: "employees",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "users",
                schema: "erp",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    username = table.Column<string>(type: "text", nullable: false),
                    password_hash = table.Column<string>(type: "text", nullable: false),
                    role = table.Column<string>(type: "text", nullable: false),
                    CompanyId = table.Column<long>(type: "bigint", nullable: true),
                    RoleId = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users", x => x.id);
                    table.ForeignKey(
                        name: "FK_users_companies_CompanyId",
                        column: x => x.CompanyId,
                        principalSchema: "erp",
                        principalTable: "companies",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_users_roles_RoleId",
                        column: x => x.RoleId,
                        principalSchema: "erp",
                        principalTable: "roles",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "unit_conversions",
                schema: "erp",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    company_id = table.Column<long>(type: "bigint", nullable: false),
                    from_unit_id = table.Column<long>(type: "bigint", nullable: false),
                    to_unit_id = table.Column<long>(type: "bigint", nullable: false),
                    conversion_factor = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("unit_conversions_pkey", x => x.id);
                    table.ForeignKey(
                        name: "unit_conversions_company_fk",
                        column: x => x.company_id,
                        principalSchema: "erp",
                        principalTable: "companies",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "unit_conversions_from_unit_fk",
                        column: x => x.from_unit_id,
                        principalSchema: "erp",
                        principalTable: "units",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "unit_conversions_to_unit_fk",
                        column: x => x.to_unit_id,
                        principalSchema: "erp",
                        principalTable: "units",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "sales_orders",
                schema: "erp",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    company_id = table.Column<long>(type: "bigint", nullable: false),
                    branch_id = table.Column<long>(type: "bigint", nullable: true),
                    customer_id = table.Column<long>(type: "bigint", nullable: true),
                    warehouse_id = table.Column<long>(type: "bigint", nullable: false),
                    order_number = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    order_date = table.Column<DateOnly>(type: "date", nullable: false, defaultValueSql: "CURRENT_DATE"),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false, defaultValueSql: "'DRAFT'"),
                    subtotal = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    discount_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    tax_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    total_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    notes = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("sales_orders_pkey", x => x.id);
                    table.ForeignKey(
                        name: "sales_orders_branch_fk",
                        column: x => x.branch_id,
                        principalSchema: "erp",
                        principalTable: "branches",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "sales_orders_company_fk",
                        column: x => x.company_id,
                        principalSchema: "erp",
                        principalTable: "companies",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "sales_orders_customer_fk",
                        column: x => x.customer_id,
                        principalSchema: "erp",
                        principalTable: "customers",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "sales_orders_warehouse_fk",
                        column: x => x.warehouse_id,
                        principalSchema: "erp",
                        principalTable: "warehouses",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "stock_transfers",
                schema: "erp",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    company_id = table.Column<long>(type: "bigint", nullable: false),
                    from_warehouse_id = table.Column<long>(type: "bigint", nullable: false),
                    to_warehouse_id = table.Column<long>(type: "bigint", nullable: false),
                    transfer_number = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    transfer_date = table.Column<DateOnly>(type: "date", nullable: false, defaultValueSql: "CURRENT_DATE"),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false, defaultValueSql: "'DRAFT'"),
                    notes = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("stock_transfers_pkey", x => x.id);
                    table.ForeignKey(
                        name: "stock_transfers_company_fk",
                        column: x => x.company_id,
                        principalSchema: "erp",
                        principalTable: "companies",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "stock_transfers_from_warehouse_fk",
                        column: x => x.from_warehouse_id,
                        principalSchema: "erp",
                        principalTable: "warehouses",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "stock_transfers_to_warehouse_fk",
                        column: x => x.to_warehouse_id,
                        principalSchema: "erp",
                        principalTable: "warehouses",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "maintenance_orders",
                schema: "erp",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    company_id = table.Column<long>(type: "bigint", nullable: false),
                    asset_id = table.Column<long>(type: "bigint", nullable: false),
                    order_number = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    maintenance_type = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false, defaultValueSql: "'CORRECTIVE'"),
                    priority = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValueSql: "'NORMAL'"),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false, defaultValueSql: "'OPEN'"),
                    scheduled_date = table.Column<DateOnly>(type: "date", nullable: true),
                    completed_date = table.Column<DateOnly>(type: "date", nullable: true),
                    description = table.Column<string>(type: "text", nullable: false),
                    technician = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    cost = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    notes = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("maintenance_orders_pkey", x => x.id);
                    table.ForeignKey(
                        name: "maintenance_orders_asset_fk",
                        column: x => x.asset_id,
                        principalSchema: "erp",
                        principalTable: "maintenance_assets",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "maintenance_orders_company_fk",
                        column: x => x.company_id,
                        principalSchema: "erp",
                        principalTable: "companies",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "asset_balances",
                schema: "erp",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    company_id = table.Column<long>(type: "bigint", nullable: false),
                    item_id = table.Column<long>(type: "bigint", nullable: false),
                    party_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    party_id = table.Column<long>(type: "bigint", nullable: false),
                    quantity = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    last_updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("asset_balances_pkey", x => x.id);
                    table.ForeignKey(
                        name: "asset_balances_company_fk",
                        column: x => x.company_id,
                        principalSchema: "erp",
                        principalTable: "companies",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "asset_balances_item_fk",
                        column: x => x.item_id,
                        principalSchema: "erp",
                        principalTable: "items",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "asset_movements",
                schema: "erp",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    company_id = table.Column<long>(type: "bigint", nullable: false),
                    movement_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_DATE"),
                    item_id = table.Column<long>(type: "bigint", nullable: false),
                    from_party_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    from_party_id = table.Column<long>(type: "bigint", nullable: false),
                    to_party_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    to_party_id = table.Column<long>(type: "bigint", nullable: false),
                    quantity = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    movement_type = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    notes = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("asset_movements_pkey", x => x.id);
                    table.ForeignKey(
                        name: "asset_movements_company_fk",
                        column: x => x.company_id,
                        principalSchema: "erp",
                        principalTable: "companies",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "asset_movements_item_fk",
                        column: x => x.item_id,
                        principalSchema: "erp",
                        principalTable: "items",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "recipes",
                schema: "erp",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    company_id = table.Column<long>(type: "bigint", nullable: false),
                    product_item_id = table.Column<long>(type: "bigint", nullable: false),
                    recipe_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    recipe_name = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    batch_quantity = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    batch_unit = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    yield_quantity = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    yield_unit = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    pieces_per_batch = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: true),
                    waste_percentage = table.Column<decimal>(type: "numeric(8,3)", precision: 8, scale: 3, nullable: false),
                    preparation_time_minutes = table.Column<int>(type: "integer", nullable: true),
                    active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    notes = table.Column<string>(type: "text", nullable: true),
                    effective_from = table.Column<DateOnly>(type: "date", nullable: false, defaultValueSql: "CURRENT_DATE"),
                    effective_to = table.Column<DateOnly>(type: "date", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("recipes_pkey", x => x.id);
                    table.ForeignKey(
                        name: "FK_recipes_items_product_item_id",
                        column: x => x.product_item_id,
                        principalSchema: "erp",
                        principalTable: "items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "recipes_company_id_fkey",
                        column: x => x.company_id,
                        principalSchema: "erp",
                        principalTable: "companies",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "stock_balances",
                schema: "erp",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    company_id = table.Column<long>(type: "bigint", nullable: false),
                    warehouse_id = table.Column<long>(type: "bigint", nullable: false),
                    item_id = table.Column<long>(type: "bigint", nullable: false),
                    quantity = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    reserved_quantity = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    available_quantity = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: true, computedColumnSql: "(quantity - reserved_quantity)", stored: true),
                    last_updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("stock_balances_pkey", x => x.id);
                    table.ForeignKey(
                        name: "stock_balances_company_id_fkey",
                        column: x => x.company_id,
                        principalSchema: "erp",
                        principalTable: "companies",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "stock_balances_item_id_fkey",
                        column: x => x.item_id,
                        principalSchema: "erp",
                        principalTable: "items",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "stock_balances_warehouse_id_fkey",
                        column: x => x.warehouse_id,
                        principalSchema: "erp",
                        principalTable: "warehouses",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "purchase_orders",
                schema: "erp",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    company_id = table.Column<long>(type: "bigint", nullable: false),
                    supplier_id = table.Column<long>(type: "bigint", nullable: false),
                    warehouse_id = table.Column<long>(type: "bigint", nullable: false),
                    order_number = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    order_date = table.Column<DateOnly>(type: "date", nullable: false, defaultValueSql: "CURRENT_DATE"),
                    expected_date = table.Column<DateOnly>(type: "date", nullable: true),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false, defaultValueSql: "'DRAFT'"),
                    subtotal = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    discount_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    tax_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    total_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    notes = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    created_by = table.Column<long>(type: "bigint", nullable: true),
                    CreatedByNavigationId = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("purchase_orders_pkey", x => x.id);
                    table.ForeignKey(
                        name: "FK_purchase_orders_users_CreatedByNavigationId",
                        column: x => x.CreatedByNavigationId,
                        principalSchema: "erp",
                        principalTable: "users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "purchase_orders_company_id_fkey",
                        column: x => x.company_id,
                        principalSchema: "erp",
                        principalTable: "companies",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "purchase_orders_supplier_id_fkey",
                        column: x => x.supplier_id,
                        principalSchema: "erp",
                        principalTable: "suppliers",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "purchase_orders_warehouse_id_fkey",
                        column: x => x.warehouse_id,
                        principalSchema: "erp",
                        principalTable: "warehouses",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "stock_movements",
                schema: "erp",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    company_id = table.Column<long>(type: "bigint", nullable: false),
                    warehouse_id = table.Column<long>(type: "bigint", nullable: false),
                    item_id = table.Column<long>(type: "bigint", nullable: false),
                    movement_type = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    quantity = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    unit_cost = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    reference_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    reference_id = table.Column<long>(type: "bigint", nullable: true),
                    notes = table.Column<string>(type: "text", nullable: true),
                    movement_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    created_by = table.Column<long>(type: "bigint", nullable: true),
                    CreatedByNavigationId = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("stock_movements_pkey", x => x.id);
                    table.ForeignKey(
                        name: "FK_stock_movements_users_CreatedByNavigationId",
                        column: x => x.CreatedByNavigationId,
                        principalSchema: "erp",
                        principalTable: "users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "stock_movements_company_id_fkey",
                        column: x => x.company_id,
                        principalSchema: "erp",
                        principalTable: "companies",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "stock_movements_item_id_fkey",
                        column: x => x.item_id,
                        principalSchema: "erp",
                        principalTable: "items",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "stock_movements_warehouse_id_fkey",
                        column: x => x.warehouse_id,
                        principalSchema: "erp",
                        principalTable: "warehouses",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "sales_order_lines",
                schema: "erp",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    sales_order_id = table.Column<long>(type: "bigint", nullable: false),
                    item_id = table.Column<long>(type: "bigint", nullable: false),
                    quantity = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    unit_price = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    discount_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    tax_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    line_total = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    delivered_quantity = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    notes = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("sales_order_lines_pkey", x => x.id);
                    table.ForeignKey(
                        name: "sales_lines_item_fk",
                        column: x => x.item_id,
                        principalSchema: "erp",
                        principalTable: "items",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "sales_lines_order_fk",
                        column: x => x.sales_order_id,
                        principalSchema: "erp",
                        principalTable: "sales_orders",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "stock_transfer_lines",
                schema: "erp",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    transfer_id = table.Column<long>(type: "bigint", nullable: false),
                    item_id = table.Column<long>(type: "bigint", nullable: false),
                    quantity = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    received_quantity = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("stock_transfer_lines_pkey", x => x.id);
                    table.ForeignKey(
                        name: "stock_transfer_lines_item_fk",
                        column: x => x.item_id,
                        principalSchema: "erp",
                        principalTable: "items",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "stock_transfer_lines_transfer_fk",
                        column: x => x.transfer_id,
                        principalSchema: "erp",
                        principalTable: "stock_transfers",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "production_orders",
                schema: "erp",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    company_id = table.Column<long>(type: "bigint", nullable: false),
                    warehouse_id = table.Column<long>(type: "bigint", nullable: false),
                    order_number = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    order_date = table.Column<DateOnly>(type: "date", nullable: false, defaultValueSql: "CURRENT_DATE"),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false, defaultValueSql: "'DRAFT'"),
                    notes = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    created_by = table.Column<long>(type: "bigint", nullable: true),
                    RecipeId = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("production_orders_pkey", x => x.id);
                    table.ForeignKey(
                        name: "FK_production_orders_recipes_RecipeId",
                        column: x => x.RecipeId,
                        principalSchema: "erp",
                        principalTable: "recipes",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "production_orders_company_fk",
                        column: x => x.company_id,
                        principalSchema: "erp",
                        principalTable: "companies",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "production_orders_warehouse_fk",
                        column: x => x.warehouse_id,
                        principalSchema: "erp",
                        principalTable: "warehouses",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "recipe_stages",
                schema: "erp",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    recipe_id = table.Column<long>(type: "bigint", nullable: false),
                    stage_number = table.Column<int>(type: "integer", nullable: false),
                    stage_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    stage_recipe_id = table.Column<long>(type: "bigint", nullable: true),
                    output_item_id = table.Column<long>(type: "bigint", nullable: true),
                    notes = table.Column<string>(type: "text", nullable: true),
                    active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("recipe_stages_pkey", x => x.id);
                    table.ForeignKey(
                        name: "recipe_stages_output_item_id_fkey",
                        column: x => x.output_item_id,
                        principalSchema: "erp",
                        principalTable: "items",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "recipe_stages_recipe_id_fkey",
                        column: x => x.recipe_id,
                        principalSchema: "erp",
                        principalTable: "recipes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "recipe_stages_stage_recipe_id_fkey",
                        column: x => x.stage_recipe_id,
                        principalSchema: "erp",
                        principalTable: "recipes",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "purchase_order_lines",
                schema: "erp",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    purchase_order_id = table.Column<long>(type: "bigint", nullable: false),
                    item_id = table.Column<long>(type: "bigint", nullable: false),
                    quantity = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    unit_price = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    discount_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    tax_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    line_total = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true, computedColumnSql: "(((quantity * unit_price) - discount_amount) + tax_amount)", stored: true),
                    received_quantity = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    notes = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("purchase_order_lines_pkey", x => x.id);
                    table.ForeignKey(
                        name: "purchase_order_lines_item_id_fkey",
                        column: x => x.item_id,
                        principalSchema: "erp",
                        principalTable: "items",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "purchase_order_lines_purchase_order_id_fkey",
                        column: x => x.purchase_order_id,
                        principalSchema: "erp",
                        principalTable: "purchase_orders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "production_order_details",
                schema: "erp",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    production_order_id = table.Column<long>(type: "bigint", nullable: false),
                    item_id = table.Column<long>(type: "bigint", nullable: false),
                    recipe_id = table.Column<long>(type: "bigint", nullable: false),
                    planned_quantity = table.Column<decimal>(type: "numeric(14,3)", precision: 14, scale: 3, nullable: false),
                    produced_quantity = table.Column<decimal>(type: "numeric(14,3)", precision: 14, scale: 3, nullable: false),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false, defaultValueSql: "'PENDING'"),
                    notes = table.Column<string>(type: "text", nullable: true),
                    Employee_id = table.Column<long>(type: "bigint", nullable: true),
                    working_hours = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    waste_quantity = table.Column<decimal>(type: "numeric(14,3)", precision: 14, scale: 3, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("production_order_details_pkey", x => x.id);
                    table.ForeignKey(
                        name: "FK_production_order_details_employees_Employee_id",
                        column: x => x.Employee_id,
                        principalSchema: "erp",
                        principalTable: "employees",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "production_order_details_item_fk",
                        column: x => x.item_id,
                        principalSchema: "erp",
                        principalTable: "items",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "production_order_details_order_fk",
                        column: x => x.production_order_id,
                        principalSchema: "erp",
                        principalTable: "production_orders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "production_order_details_recipe_fk",
                        column: x => x.recipe_id,
                        principalSchema: "erp",
                        principalTable: "recipes",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "productivity_logs",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    production_order_id = table.Column<long>(type: "bigint", nullable: false),
                    employee_id = table.Column<long>(type: "bigint", nullable: false),
                    work_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    hours_worked = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    produced_quantity = table.Column<decimal>(type: "numeric(18,3)", nullable: false),
                    notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_productivity_logs", x => x.id);
                    table.ForeignKey(
                        name: "FK_productivity_logs_employees_employee_id",
                        column: x => x.employee_id,
                        principalSchema: "erp",
                        principalTable: "employees",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_productivity_logs_production_orders_production_order_id",
                        column: x => x.production_order_id,
                        principalSchema: "erp",
                        principalTable: "production_orders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "recipe_lines",
                schema: "erp",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    recipe_id = table.Column<long>(type: "bigint", nullable: false),
                    component_item_id = table.Column<long>(type: "bigint", nullable: false),
                    line_number = table.Column<int>(type: "integer", nullable: false),
                    quantity = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    unit = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    waste_percentage = table.Column<decimal>(type: "numeric(8,3)", precision: 8, scale: 3, nullable: false),
                    is_optional = table.Column<bool>(type: "boolean", nullable: false),
                    notes = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    stage_id = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("recipe_lines_pkey", x => x.id);
                    table.ForeignKey(
                        name: "recipe_lines_component_item_id_fkey",
                        column: x => x.component_item_id,
                        principalSchema: "erp",
                        principalTable: "items",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "recipe_lines_recipe_id_fkey",
                        column: x => x.recipe_id,
                        principalSchema: "erp",
                        principalTable: "recipes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "recipe_lines_stage_fk",
                        column: x => x.stage_id,
                        principalSchema: "erp",
                        principalTable: "recipe_stages",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "production_order_stages",
                schema: "erp",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    production_order_detail_id = table.Column<long>(type: "bigint", nullable: false),
                    stage_number = table.Column<int>(type: "integer", nullable: false),
                    item_id = table.Column<long>(type: "bigint", nullable: false),
                    recipe_stage_id = table.Column<long>(type: "bigint", nullable: true),
                    planned_quantity = table.Column<decimal>(type: "numeric(14,3)", precision: 14, scale: 3, nullable: false),
                    produced_quantity = table.Column<decimal>(type: "numeric(14,3)", precision: 14, scale: 3, nullable: false),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false, defaultValueSql: "'PENDING'"),
                    notes = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    RecipeId = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("production_order_stages_pkey", x => x.id);
                    table.ForeignKey(
                        name: "FK_production_order_stages_recipes_RecipeId",
                        column: x => x.RecipeId,
                        principalSchema: "erp",
                        principalTable: "recipes",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "production_stages_detail_fk",
                        column: x => x.production_order_detail_id,
                        principalSchema: "erp",
                        principalTable: "production_order_details",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "production_stages_item_fk",
                        column: x => x.item_id,
                        principalSchema: "erp",
                        principalTable: "items",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "production_stages_recipe_stage_fk",
                        column: x => x.recipe_stage_id,
                        principalSchema: "erp",
                        principalTable: "recipe_stages",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "production_stage_materials",
                schema: "erp",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    production_stage_id = table.Column<long>(type: "bigint", nullable: false),
                    item_id = table.Column<long>(type: "bigint", nullable: false),
                    required_quantity = table.Column<decimal>(type: "numeric(14,3)", precision: 14, scale: 3, nullable: false),
                    consumed_quantity = table.Column<decimal>(type: "numeric(14,3)", precision: 14, scale: 3, nullable: false),
                    unit = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    waste_percentage = table.Column<decimal>(type: "numeric(8,3)", precision: 8, scale: 3, nullable: false),
                    notes = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    recipe_line_id = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("production_stage_materials_pkey", x => x.id);
                    table.ForeignKey(
                        name: "production_material_recipe_line_fk",
                        column: x => x.recipe_line_id,
                        principalSchema: "erp",
                        principalTable: "recipe_lines",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "production_materials_item_fk",
                        column: x => x.item_id,
                        principalSchema: "erp",
                        principalTable: "items",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "production_materials_stage_fk",
                        column: x => x.production_stage_id,
                        principalSchema: "erp",
                        principalTable: "production_order_stages",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "quality_inspections",
                schema: "erp",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    company_id = table.Column<long>(type: "bigint", nullable: false),
                    item_id = table.Column<long>(type: "bigint", nullable: false),
                    warehouse_id = table.Column<long>(type: "bigint", nullable: true),
                    production_order_id = table.Column<long>(type: "bigint", nullable: true),
                    production_stage_id = table.Column<long>(type: "bigint", nullable: true),
                    inspection_type = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    inspection_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    quantity = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false, defaultValueSql: "'PENDING'"),
                    result = table.Column<string>(type: "text", nullable: true),
                    notes = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    IsGmpPassed = table.Column<bool>(type: "boolean", nullable: false),
                    IsHaccpPassed = table.Column<bool>(type: "boolean", nullable: false),
                    IsWeightsPassed = table.Column<bool>(type: "boolean", nullable: false),
                    QualityScore = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    InspectorName = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("quality_inspections_pkey", x => x.id);
                    table.ForeignKey(
                        name: "quality_company_fk",
                        column: x => x.company_id,
                        principalSchema: "erp",
                        principalTable: "companies",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "quality_inspections_item_id_fkey",
                        column: x => x.item_id,
                        principalSchema: "erp",
                        principalTable: "items",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "quality_inspections_production_order_id_fkey",
                        column: x => x.production_order_id,
                        principalSchema: "erp",
                        principalTable: "production_orders",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "quality_inspections_production_stage_id_fkey",
                        column: x => x.production_stage_id,
                        principalSchema: "erp",
                        principalTable: "production_order_stages",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "quality_inspections_warehouse_id_fkey",
                        column: x => x.warehouse_id,
                        principalSchema: "erp",
                        principalTable: "warehouses",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "waste_records",
                schema: "erp",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    company_id = table.Column<long>(type: "bigint", nullable: false),
                    item_id = table.Column<long>(type: "bigint", nullable: false),
                    warehouse_id = table.Column<long>(type: "bigint", nullable: true),
                    production_order_id = table.Column<long>(type: "bigint", nullable: true),
                    production_stage_id = table.Column<long>(type: "bigint", nullable: true),
                    quantity = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    waste_reason = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    waste_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    notes = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    created_by = table.Column<long>(type: "bigint", nullable: true),
                    CreatedByNavigationId = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("waste_records_pkey", x => x.id);
                    table.ForeignKey(
                        name: "FK_waste_records_users_CreatedByNavigationId",
                        column: x => x.CreatedByNavigationId,
                        principalSchema: "erp",
                        principalTable: "users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "waste_company_fk",
                        column: x => x.company_id,
                        principalSchema: "erp",
                        principalTable: "companies",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "waste_item_fk",
                        column: x => x.item_id,
                        principalSchema: "erp",
                        principalTable: "items",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "waste_production_order_fk",
                        column: x => x.production_order_id,
                        principalSchema: "erp",
                        principalTable: "production_orders",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "waste_production_stage_fk",
                        column: x => x.production_stage_id,
                        principalSchema: "erp",
                        principalTable: "production_order_stages",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "waste_records_warehouse_id_fkey",
                        column: x => x.warehouse_id,
                        principalSchema: "erp",
                        principalTable: "warehouses",
                        principalColumn: "id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_asset_balances_item_id",
                schema: "erp",
                table: "asset_balances",
                column: "item_id");

            migrationBuilder.CreateIndex(
                name: "ux_asset_balances_unique",
                schema: "erp",
                table: "asset_balances",
                columns: new[] { "company_id", "item_id", "party_type", "party_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_asset_movements_from",
                schema: "erp",
                table: "asset_movements",
                columns: new[] { "from_party_type", "from_party_id" });

            migrationBuilder.CreateIndex(
                name: "idx_asset_movements_item",
                schema: "erp",
                table: "asset_movements",
                column: "item_id");

            migrationBuilder.CreateIndex(
                name: "idx_asset_movements_to",
                schema: "erp",
                table: "asset_movements",
                columns: new[] { "to_party_type", "to_party_id" });

            migrationBuilder.CreateIndex(
                name: "IX_asset_movements_company_id",
                schema: "erp",
                table: "asset_movements",
                column: "company_id");

            migrationBuilder.CreateIndex(
                name: "branches_company_id_branch_code_key",
                schema: "erp",
                table: "branches",
                columns: new[] { "company_id", "branch_code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_branches_company_id",
                schema: "erp",
                table: "branches",
                column: "company_id");

            migrationBuilder.CreateIndex(
                name: "ux_branches_company_code",
                schema: "erp",
                table: "branches",
                columns: new[] { "company_id", "branch_code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "companies_company_code_key",
                schema: "erp",
                table: "companies",
                column: "company_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_companies_company_code",
                schema: "erp",
                table: "companies",
                column: "company_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_crate_transactions_crate_id",
                schema: "erp",
                table: "crate_transactions",
                column: "crate_id");

            migrationBuilder.CreateIndex(
                name: "IX_CrateClientTransactions_CrateClientId",
                table: "CrateClientTransactions",
                column: "CrateClientId");

            migrationBuilder.CreateIndex(
                name: "customers_company_id_customer_code_key",
                schema: "erp",
                table: "customers",
                columns: new[] { "company_id", "customer_code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_customers_company_id",
                schema: "erp",
                table: "customers",
                column: "company_id");

            migrationBuilder.CreateIndex(
                name: "ux_customers_company_code",
                schema: "erp",
                table: "customers",
                columns: new[] { "company_id", "customer_code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "departments_company_id_department_code_key",
                schema: "erp",
                table: "departments",
                columns: new[] { "company_id", "department_code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_departments_parent_department_id",
                schema: "erp",
                table: "departments",
                column: "parent_department_id");

            migrationBuilder.CreateIndex(
                name: "dynamic_forms_form_code_key",
                schema: "erp",
                table: "dynamic_forms",
                column: "form_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_employees_branch_id",
                schema: "erp",
                table: "employees",
                column: "branch_id");

            migrationBuilder.CreateIndex(
                name: "IX_employees_company_id",
                schema: "erp",
                table: "employees",
                column: "company_id");

            migrationBuilder.CreateIndex(
                name: "IX_employees_department_id",
                schema: "erp",
                table: "employees",
                column: "department_id");

            migrationBuilder.CreateIndex(
                name: "IX_employees_job_title_id",
                schema: "erp",
                table: "employees",
                column: "job_title_id");

            migrationBuilder.CreateIndex(
                name: "IX_employees_manager_id",
                schema: "erp",
                table: "employees",
                column: "manager_id");

            migrationBuilder.CreateIndex(
                name: "IX_form_submissions_form_id",
                schema: "erp",
                table: "form_submissions",
                column: "form_id");

            migrationBuilder.CreateIndex(
                name: "item_categories_company_id_category_code_key",
                schema: "erp",
                table: "item_categories",
                columns: new[] { "company_id", "category_code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_item_categories_company_id",
                schema: "erp",
                table: "item_categories",
                column: "company_id");

            migrationBuilder.CreateIndex(
                name: "ux_item_categories_company_code",
                schema: "erp",
                table: "item_categories",
                columns: new[] { "company_id", "category_code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_items_category_id",
                schema: "erp",
                table: "items",
                column: "category_id");

            migrationBuilder.CreateIndex(
                name: "IX_items_company_id",
                schema: "erp",
                table: "items",
                column: "company_id");

            migrationBuilder.CreateIndex(
                name: "ux_job_titles_company_title",
                schema: "erp",
                table: "job_titles",
                columns: new[] { "company_id", "title_name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_journal_entry_lines_account_id",
                table: "journal_entry_lines",
                column: "account_id");

            migrationBuilder.CreateIndex(
                name: "IX_journal_entry_lines_journal_entry_id",
                table: "journal_entry_lines",
                column: "journal_entry_id");

            migrationBuilder.CreateIndex(
                name: "IX_maintenance_assets_department_id",
                schema: "erp",
                table: "maintenance_assets",
                column: "department_id");

            migrationBuilder.CreateIndex(
                name: "maintenance_assets_company_id_asset_code_key",
                schema: "erp",
                table: "maintenance_assets",
                columns: new[] { "company_id", "asset_code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_maintenance_asset",
                schema: "erp",
                table: "maintenance_orders",
                column: "asset_id");

            migrationBuilder.CreateIndex(
                name: "maintenance_orders_company_id_order_number_key",
                schema: "erp",
                table: "maintenance_orders",
                columns: new[] { "company_id", "order_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_production_order_details_Employee_id",
                schema: "erp",
                table: "production_order_details",
                column: "Employee_id");

            migrationBuilder.CreateIndex(
                name: "IX_production_order_details_item_id",
                schema: "erp",
                table: "production_order_details",
                column: "item_id");

            migrationBuilder.CreateIndex(
                name: "IX_production_order_details_production_order_id",
                schema: "erp",
                table: "production_order_details",
                column: "production_order_id");

            migrationBuilder.CreateIndex(
                name: "IX_production_order_details_recipe_id",
                schema: "erp",
                table: "production_order_details",
                column: "recipe_id");

            migrationBuilder.CreateIndex(
                name: "idx_production_stages_detail",
                schema: "erp",
                table: "production_order_stages",
                column: "production_order_detail_id");

            migrationBuilder.CreateIndex(
                name: "idx_production_stages_item",
                schema: "erp",
                table: "production_order_stages",
                column: "item_id");

            migrationBuilder.CreateIndex(
                name: "idx_production_stages_recipe_stage",
                schema: "erp",
                table: "production_order_stages",
                column: "recipe_stage_id");

            migrationBuilder.CreateIndex(
                name: "IX_production_order_stages_RecipeId",
                schema: "erp",
                table: "production_order_stages",
                column: "RecipeId");

            migrationBuilder.CreateIndex(
                name: "production_stages_unique",
                schema: "erp",
                table: "production_order_stages",
                columns: new[] { "production_order_detail_id", "stage_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_production_orders_status",
                schema: "erp",
                table: "production_orders",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "IX_production_orders_RecipeId",
                schema: "erp",
                table: "production_orders",
                column: "RecipeId");

            migrationBuilder.CreateIndex(
                name: "IX_production_orders_warehouse_id",
                schema: "erp",
                table: "production_orders",
                column: "warehouse_id");

            migrationBuilder.CreateIndex(
                name: "production_orders_company_order_number_unique",
                schema: "erp",
                table: "production_orders",
                columns: new[] { "company_id", "order_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_production_material_recipe_line",
                schema: "erp",
                table: "production_stage_materials",
                column: "recipe_line_id");

            migrationBuilder.CreateIndex(
                name: "idx_production_materials_item",
                schema: "erp",
                table: "production_stage_materials",
                column: "item_id");

            migrationBuilder.CreateIndex(
                name: "idx_production_materials_stage",
                schema: "erp",
                table: "production_stage_materials",
                column: "production_stage_id");

            migrationBuilder.CreateIndex(
                name: "IX_productivity_logs_employee_id",
                table: "productivity_logs",
                column: "employee_id");

            migrationBuilder.CreateIndex(
                name: "IX_productivity_logs_production_order_id",
                table: "productivity_logs",
                column: "production_order_id");

            migrationBuilder.CreateIndex(
                name: "IX_purchase_invoice_lines_purchase_invoice_id",
                schema: "erp",
                table: "purchase_invoice_lines",
                column: "purchase_invoice_id");

            migrationBuilder.CreateIndex(
                name: "IX_purchase_order_lines_item_id",
                schema: "erp",
                table: "purchase_order_lines",
                column: "item_id");

            migrationBuilder.CreateIndex(
                name: "IX_purchase_order_lines_purchase_order_id",
                schema: "erp",
                table: "purchase_order_lines",
                column: "purchase_order_id");

            migrationBuilder.CreateIndex(
                name: "IX_purchase_orders_CreatedByNavigationId",
                schema: "erp",
                table: "purchase_orders",
                column: "CreatedByNavigationId");

            migrationBuilder.CreateIndex(
                name: "IX_purchase_orders_supplier_id",
                schema: "erp",
                table: "purchase_orders",
                column: "supplier_id");

            migrationBuilder.CreateIndex(
                name: "IX_purchase_orders_warehouse_id",
                schema: "erp",
                table: "purchase_orders",
                column: "warehouse_id");

            migrationBuilder.CreateIndex(
                name: "purchase_orders_company_id_order_number_key",
                schema: "erp",
                table: "purchase_orders",
                columns: new[] { "company_id", "order_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_quality_item",
                schema: "erp",
                table: "quality_inspections",
                column: "item_id");

            migrationBuilder.CreateIndex(
                name: "idx_quality_production",
                schema: "erp",
                table: "quality_inspections",
                column: "production_order_id");

            migrationBuilder.CreateIndex(
                name: "IX_quality_inspections_company_id",
                schema: "erp",
                table: "quality_inspections",
                column: "company_id");

            migrationBuilder.CreateIndex(
                name: "IX_quality_inspections_production_stage_id",
                schema: "erp",
                table: "quality_inspections",
                column: "production_stage_id");

            migrationBuilder.CreateIndex(
                name: "IX_quality_inspections_warehouse_id",
                schema: "erp",
                table: "quality_inspections",
                column: "warehouse_id");

            migrationBuilder.CreateIndex(
                name: "idx_recipe_lines_stage",
                schema: "erp",
                table: "recipe_lines",
                column: "stage_id");

            migrationBuilder.CreateIndex(
                name: "IX_recipe_lines_component_item_id",
                schema: "erp",
                table: "recipe_lines",
                column: "component_item_id");

            migrationBuilder.CreateIndex(
                name: "recipe_lines_recipe_id_line_number_key",
                schema: "erp",
                table: "recipe_lines",
                columns: new[] { "recipe_id", "line_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_recipe_stages_output_item",
                schema: "erp",
                table: "recipe_stages",
                column: "output_item_id");

            migrationBuilder.CreateIndex(
                name: "idx_recipe_stages_recipe",
                schema: "erp",
                table: "recipe_stages",
                column: "recipe_id");

            migrationBuilder.CreateIndex(
                name: "idx_recipe_stages_stage_recipe",
                schema: "erp",
                table: "recipe_stages",
                column: "stage_recipe_id");

            migrationBuilder.CreateIndex(
                name: "recipe_stages_recipe_id_stage_number_key",
                schema: "erp",
                table: "recipe_stages",
                columns: new[] { "recipe_id", "stage_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_recipes_product_item_id",
                schema: "erp",
                table: "recipes",
                column: "product_item_id");

            migrationBuilder.CreateIndex(
                name: "recipes_company_id_recipe_code_key",
                schema: "erp",
                table: "recipes",
                columns: new[] { "company_id", "recipe_code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_roles_company",
                schema: "erp",
                table: "roles",
                column: "company_id");

            migrationBuilder.CreateIndex(
                name: "roles_company_id_role_code_key",
                schema: "erp",
                table: "roles",
                columns: new[] { "company_id", "role_code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_sales_invoice_lines_sales_invoice_id",
                schema: "erp",
                table: "sales_invoice_lines",
                column: "sales_invoice_id");

            migrationBuilder.CreateIndex(
                name: "idx_sales_lines_item",
                schema: "erp",
                table: "sales_order_lines",
                column: "item_id");

            migrationBuilder.CreateIndex(
                name: "idx_sales_lines_order",
                schema: "erp",
                table: "sales_order_lines",
                column: "sales_order_id");

            migrationBuilder.CreateIndex(
                name: "idx_sales_orders_company",
                schema: "erp",
                table: "sales_orders",
                column: "company_id");

            migrationBuilder.CreateIndex(
                name: "idx_sales_orders_customer",
                schema: "erp",
                table: "sales_orders",
                column: "customer_id");

            migrationBuilder.CreateIndex(
                name: "idx_sales_orders_warehouse",
                schema: "erp",
                table: "sales_orders",
                column: "warehouse_id");

            migrationBuilder.CreateIndex(
                name: "IX_sales_orders_branch_id",
                schema: "erp",
                table: "sales_orders",
                column: "branch_id");

            migrationBuilder.CreateIndex(
                name: "sales_orders_company_id_order_number_key",
                schema: "erp",
                table: "sales_orders",
                columns: new[] { "company_id", "order_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_stock_balances_company",
                schema: "erp",
                table: "stock_balances",
                column: "company_id");

            migrationBuilder.CreateIndex(
                name: "idx_stock_balances_item",
                schema: "erp",
                table: "stock_balances",
                column: "item_id");

            migrationBuilder.CreateIndex(
                name: "idx_stock_balances_warehouse",
                schema: "erp",
                table: "stock_balances",
                column: "warehouse_id");

            migrationBuilder.CreateIndex(
                name: "stock_balances_warehouse_id_item_id_key",
                schema: "erp",
                table: "stock_balances",
                columns: new[] { "warehouse_id", "item_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_stock_movements_company",
                schema: "erp",
                table: "stock_movements",
                column: "company_id");

            migrationBuilder.CreateIndex(
                name: "idx_stock_movements_date",
                schema: "erp",
                table: "stock_movements",
                column: "movement_at");

            migrationBuilder.CreateIndex(
                name: "idx_stock_movements_item",
                schema: "erp",
                table: "stock_movements",
                column: "item_id");

            migrationBuilder.CreateIndex(
                name: "idx_stock_movements_reference",
                schema: "erp",
                table: "stock_movements",
                columns: new[] { "reference_type", "reference_id" });

            migrationBuilder.CreateIndex(
                name: "idx_stock_movements_warehouse",
                schema: "erp",
                table: "stock_movements",
                column: "warehouse_id");

            migrationBuilder.CreateIndex(
                name: "IX_stock_movements_CreatedByNavigationId",
                schema: "erp",
                table: "stock_movements",
                column: "CreatedByNavigationId");

            migrationBuilder.CreateIndex(
                name: "idx_stock_transfer_lines_item",
                schema: "erp",
                table: "stock_transfer_lines",
                column: "item_id");

            migrationBuilder.CreateIndex(
                name: "IX_stock_transfer_lines_transfer_id",
                schema: "erp",
                table: "stock_transfer_lines",
                column: "transfer_id");

            migrationBuilder.CreateIndex(
                name: "idx_stock_transfers_from",
                schema: "erp",
                table: "stock_transfers",
                column: "from_warehouse_id");

            migrationBuilder.CreateIndex(
                name: "idx_stock_transfers_to",
                schema: "erp",
                table: "stock_transfers",
                column: "to_warehouse_id");

            migrationBuilder.CreateIndex(
                name: "stock_transfers_company_id_transfer_number_key",
                schema: "erp",
                table: "stock_transfers",
                columns: new[] { "company_id", "transfer_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_suppliers_company_id",
                schema: "erp",
                table: "suppliers",
                column: "company_id");

            migrationBuilder.CreateIndex(
                name: "suppliers_company_id_supplier_code_key",
                schema: "erp",
                table: "suppliers",
                columns: new[] { "company_id", "supplier_code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_suppliers_company_code",
                schema: "erp",
                table: "suppliers",
                columns: new[] { "company_id", "supplier_code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_unit_conversions_from",
                schema: "erp",
                table: "unit_conversions",
                column: "from_unit_id");

            migrationBuilder.CreateIndex(
                name: "idx_unit_conversions_to",
                schema: "erp",
                table: "unit_conversions",
                column: "to_unit_id");

            migrationBuilder.CreateIndex(
                name: "unit_conversions_company_id_from_unit_id_to_unit_id_key",
                schema: "erp",
                table: "unit_conversions",
                columns: new[] { "company_id", "from_unit_id", "to_unit_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_units_company",
                schema: "erp",
                table: "units",
                column: "company_id");

            migrationBuilder.CreateIndex(
                name: "units_company_id_unit_code_key",
                schema: "erp",
                table: "units",
                columns: new[] { "company_id", "unit_code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_users_CompanyId",
                schema: "erp",
                table: "users",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_users_RoleId",
                schema: "erp",
                table: "users",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_warehouses_branch_id",
                schema: "erp",
                table: "warehouses",
                column: "branch_id");

            migrationBuilder.CreateIndex(
                name: "IX_warehouses_company_id",
                schema: "erp",
                table: "warehouses",
                column: "company_id");

            migrationBuilder.CreateIndex(
                name: "idx_waste_production",
                schema: "erp",
                table: "waste_records",
                column: "production_order_id");

            migrationBuilder.CreateIndex(
                name: "IX_waste_records_company_id",
                schema: "erp",
                table: "waste_records",
                column: "company_id");

            migrationBuilder.CreateIndex(
                name: "IX_waste_records_CreatedByNavigationId",
                schema: "erp",
                table: "waste_records",
                column: "CreatedByNavigationId");

            migrationBuilder.CreateIndex(
                name: "IX_waste_records_item_id",
                schema: "erp",
                table: "waste_records",
                column: "item_id");

            migrationBuilder.CreateIndex(
                name: "IX_waste_records_production_stage_id",
                schema: "erp",
                table: "waste_records",
                column: "production_stage_id");

            migrationBuilder.CreateIndex(
                name: "IX_waste_records_warehouse_id",
                schema: "erp",
                table: "waste_records",
                column: "warehouse_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "asset_balances",
                schema: "erp");

            migrationBuilder.DropTable(
                name: "asset_movements",
                schema: "erp");

            migrationBuilder.DropTable(
                name: "crate_transactions",
                schema: "erp");

            migrationBuilder.DropTable(
                name: "CrateClientTransactions");

            migrationBuilder.DropTable(
                name: "form_submissions",
                schema: "erp");

            migrationBuilder.DropTable(
                name: "journal_entry_lines");

            migrationBuilder.DropTable(
                name: "maintenance_orders",
                schema: "erp");

            migrationBuilder.DropTable(
                name: "production_stage_materials",
                schema: "erp");

            migrationBuilder.DropTable(
                name: "productivity_logs");

            migrationBuilder.DropTable(
                name: "purchase_invoice_lines",
                schema: "erp");

            migrationBuilder.DropTable(
                name: "purchase_order_lines",
                schema: "erp");

            migrationBuilder.DropTable(
                name: "quality_inspections",
                schema: "erp");

            migrationBuilder.DropTable(
                name: "sales_invoice_lines",
                schema: "erp");

            migrationBuilder.DropTable(
                name: "sales_order_lines",
                schema: "erp");

            migrationBuilder.DropTable(
                name: "stock_balances",
                schema: "erp");

            migrationBuilder.DropTable(
                name: "stock_movements",
                schema: "erp");

            migrationBuilder.DropTable(
                name: "stock_transfer_lines",
                schema: "erp");

            migrationBuilder.DropTable(
                name: "unit_conversions",
                schema: "erp");

            migrationBuilder.DropTable(
                name: "waste_records",
                schema: "erp");

            migrationBuilder.DropTable(
                name: "crates",
                schema: "erp");

            migrationBuilder.DropTable(
                name: "CrateClients");

            migrationBuilder.DropTable(
                name: "dynamic_forms",
                schema: "erp");

            migrationBuilder.DropTable(
                name: "accounts");

            migrationBuilder.DropTable(
                name: "journal_entries");

            migrationBuilder.DropTable(
                name: "maintenance_assets",
                schema: "erp");

            migrationBuilder.DropTable(
                name: "recipe_lines",
                schema: "erp");

            migrationBuilder.DropTable(
                name: "purchase_invoices",
                schema: "erp");

            migrationBuilder.DropTable(
                name: "purchase_orders",
                schema: "erp");

            migrationBuilder.DropTable(
                name: "sales_invoices",
                schema: "erp");

            migrationBuilder.DropTable(
                name: "sales_orders",
                schema: "erp");

            migrationBuilder.DropTable(
                name: "stock_transfers",
                schema: "erp");

            migrationBuilder.DropTable(
                name: "units",
                schema: "erp");

            migrationBuilder.DropTable(
                name: "production_order_stages",
                schema: "erp");

            migrationBuilder.DropTable(
                name: "users",
                schema: "erp");

            migrationBuilder.DropTable(
                name: "suppliers",
                schema: "erp");

            migrationBuilder.DropTable(
                name: "customers",
                schema: "erp");

            migrationBuilder.DropTable(
                name: "production_order_details",
                schema: "erp");

            migrationBuilder.DropTable(
                name: "recipe_stages",
                schema: "erp");

            migrationBuilder.DropTable(
                name: "roles",
                schema: "erp");

            migrationBuilder.DropTable(
                name: "employees",
                schema: "erp");

            migrationBuilder.DropTable(
                name: "production_orders",
                schema: "erp");

            migrationBuilder.DropTable(
                name: "job_titles",
                schema: "erp");

            migrationBuilder.DropTable(
                name: "departments",
                schema: "erp");

            migrationBuilder.DropTable(
                name: "recipes",
                schema: "erp");

            migrationBuilder.DropTable(
                name: "warehouses",
                schema: "erp");

            migrationBuilder.DropTable(
                name: "items",
                schema: "erp");

            migrationBuilder.DropTable(
                name: "branches",
                schema: "erp");

            migrationBuilder.DropTable(
                name: "item_categories",
                schema: "erp");

            migrationBuilder.DropTable(
                name: "companies",
                schema: "erp");

            migrationBuilder.DropSequence(
                name: "production_order_number_seq",
                schema: "erp");
        }
    }
}
