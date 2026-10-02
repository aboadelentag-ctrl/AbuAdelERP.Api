using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace AbuAdelERP.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddFixedAssetsAndCratesTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. إنشاء جدول الأصول الثابتة والماكينات
            migrationBuilder.CreateTable(
                name: "fixed_assets",
                schema: "erp",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    asset_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    department = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    category = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    brand_model = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    serial_number = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    custodian_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    custodian_name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    installation_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    active_sos_task_number = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_fixed_assets", x => x.id);
                });

            // 2. إنشاء جدول السجل التاريخي للماكينة (Asset Dossier)
            migrationBuilder.CreateTable(
                name: "asset_history_logs",
                schema: "erp",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    fixed_asset_id = table.Column<long>(type: "bigint", nullable: false),
                    date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    event_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    performed_by_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    performed_by_name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    cost_impact = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_asset_history_logs", x => x.id);
                    table.ForeignKey(
                        name: "FK_asset_history_logs_fixed_assets_fixed_asset_id",
                        column: x => x.fixed_asset_id,
                        principalSchema: "erp",
                        principalTable: "fixed_assets",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            // 3. إنشاء جدول أرصدة الأسبات والصواني المتداولة
            migrationBuilder.CreateTable(
                name: "circulating_containers",
                schema: "erp",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    container_name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    specification = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    total_factory_quantity = table.Column<int>(type: "integer", nullable: false),
                    in_production_floor_quantity = table.Column<int>(type: "integer", nullable: false),
                    in_packaging_warehouse_quantity = table.Column<int>(type: "integer", nullable: false),
                    with_drivers_and_branches_quantity = table.Column<int>(type: "integer", nullable: false),
                    disputed_quantity = table.Column<int>(type: "integer", nullable: false),
                    damaged_quantity = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_circulating_containers", x => x.id);
                });

            // 4. إنشاء جدول دورة استلام وتسليم الأسبات وفض المنازعات
            migrationBuilder.CreateTable(
                name: "crate_movements",
                schema: "erp",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    movement_number = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    movement_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    container_name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    dispatched_quantity = table.Column<int>(type: "integer", nullable: false),
                    source_location = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    dispatched_by_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    dispatched_by_name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    destination_location = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    received_quantity = table.Column<int>(type: "integer", nullable: true),
                    shortage_quantity = table.Column<int>(type: "integer", nullable: false),
                    damaged_count = table.Column<int>(type: "integer", nullable: false),
                    received_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    received_by_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    received_by_name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    receive_notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    arbitration_decision = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    arbitration_text = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    arbitrated_by_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    arbitrated_by_name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    financial_penalty = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_crate_movements", x => x.id);
                });

            // الفهارس الفريدة
            migrationBuilder.CreateIndex(
                name: "IX_fixed_assets_asset_code",
                schema: "erp",
                table: "fixed_assets",
                column: "asset_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_asset_history_logs_fixed_asset_id",
                schema: "erp",
                table: "asset_history_logs",
                column: "fixed_asset_id");

            migrationBuilder.CreateIndex(
                name: "IX_circulating_containers_container_name",
                schema: "erp",
                table: "circulating_containers",
                column: "container_name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_crate_movements_movement_number",
                schema: "erp",
                table: "crate_movements",
                column: "movement_number",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "asset_history_logs",
                schema: "erp");

            migrationBuilder.DropTable(
                name: "fixed_assets",
                schema: "erp");

            migrationBuilder.DropTable(
                name: "crate_movements",
                schema: "erp");

            migrationBuilder.DropTable(
                name: "circulating_containers",
                schema: "erp");
        }
    }
}