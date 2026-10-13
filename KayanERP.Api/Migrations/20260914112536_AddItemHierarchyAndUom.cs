using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace KayanERP.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddItemHierarchyAndUom : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // تم الإبقاء فقط على تفرعات الأصناف
            migrationBuilder.AddColumn<long>(
                name: "parent_item_id",
                schema: "erp",
                table: "items",
                type: "bigint",
                nullable: true);

            // إنشاء جدول معاملات تحويل الوحدات
            migrationBuilder.CreateTable(
                name: "item_uom_conversions",
                schema: "erp",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    company_id = table.Column<long>(type: "bigint", nullable: false),
                    item_id = table.Column<long>(type: "bigint", nullable: true),
                    from_unit = table.Column<string>(type: "text", nullable: false),
                    to_unit = table.Column<string>(type: "text", nullable: false),
                    conversion_factor = table.Column<decimal>(type: "numeric", nullable: false),
                    notes = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_item_uom_conversions", x => x.id);
                    table.ForeignKey(
                        name: "FK_item_uom_conversions_items_item_id",
                        column: x => x.item_id,
                        principalSchema: "erp",
                        principalTable: "items",
                        principalColumn: "id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_items_parent_item_id",
                schema: "erp",
                table: "items",
                column: "parent_item_id");

            migrationBuilder.CreateIndex(
                name: "IX_item_uom_conversions_item_id",
                schema: "erp",
                table: "item_uom_conversions",
                column: "item_id");

            migrationBuilder.AddForeignKey(
                name: "FK_items_items_parent_item_id",
                schema: "erp",
                table: "items",
                column: "parent_item_id",
                principalSchema: "erp",
                principalTable: "items",
                principalColumn: "id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_items_items_parent_item_id",
                schema: "erp",
                table: "items");

            migrationBuilder.DropTable(
                name: "item_uom_conversions",
                schema: "erp");

            migrationBuilder.DropIndex(
                name: "IX_items_parent_item_id",
                schema: "erp",
                table: "items");

            migrationBuilder.DropColumn(
                name: "parent_item_id",
                schema: "erp",
                table: "items");
        }
    }
}