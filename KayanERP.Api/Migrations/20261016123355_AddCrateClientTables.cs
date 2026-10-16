using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace KayanERP.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddCrateClientTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "crate_clients",
                schema: "erp",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    company_id = table.Column<long>(type: "bigint", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    crate_balance = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("crate_clients_pkey", x => x.id);
                    table.ForeignKey(
                        name: "crate_clients_company_id_fkey",
                        column: x => x.company_id,
                        principalSchema: "erp",
                        principalTable: "companies",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "crate_client_transactions",
                schema: "erp",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    company_id = table.Column<long>(type: "bigint", nullable: false),
                    crate_client_id = table.Column<long>(type: "bigint", nullable: false),
                    transaction_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    quantity = table.Column<int>(type: "integer", nullable: false),
                    transaction_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("crate_client_transactions_pkey", x => x.id);
                    table.ForeignKey(
                        name: "crate_client_transactions_company_id_fkey",
                        column: x => x.company_id,
                        principalSchema: "erp",
                        principalTable: "companies",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "crate_client_transactions_crate_client_id_fkey",
                        column: x => x.crate_client_id,
                        principalSchema: "erp",
                        principalTable: "crate_clients",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_crate_client_transactions_company_id",
                schema: "erp",
                table: "crate_client_transactions",
                column: "company_id");

            migrationBuilder.CreateIndex(
                name: "ix_crate_client_transactions_crate_client_id",
                schema: "erp",
                table: "crate_client_transactions",
                column: "crate_client_id");

            migrationBuilder.CreateIndex(
                name: "ix_crate_client_transactions_transaction_date",
                schema: "erp",
                table: "crate_client_transactions",
                column: "transaction_date");

            migrationBuilder.CreateIndex(
                name: "ix_crate_clients_company_id",
                schema: "erp",
                table: "crate_clients",
                column: "company_id");

            migrationBuilder.CreateIndex(
                name: "ix_crate_clients_company_id_name",
                schema: "erp",
                table: "crate_clients",
                columns: new[] { "company_id", "name" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "crate_client_transactions",
                schema: "erp");

            migrationBuilder.DropTable(
                name: "crate_clients",
                schema: "erp");
        }
    }
}
