using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AbuAdelERP.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddDepartmentToItems : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<long>(
                name: "recipe_id",
                schema: "erp",
                table: "production_order_details",
                type: "bigint",
                nullable: true,
                oldClrType: typeof(long),
                oldType: "bigint");

            migrationBuilder.AddColumn<string>(
                name: "BatchNumber",
                schema: "erp",
                table: "production_order_details",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                schema: "erp",
                table: "production_order_details",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "Unit",
                schema: "erp",
                table: "production_order_details",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                schema: "erp",
                table: "production_order_details",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<long>(
                name: "department_id",
                schema: "erp",
                table: "items",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_items_department_id",
                schema: "erp",
                table: "items",
                column: "department_id");

            migrationBuilder.AddForeignKey(
                name: "FK_items_departments_department_id",
                schema: "erp",
                table: "items",
                column: "department_id",
                principalSchema: "erp",
                principalTable: "departments",
                principalColumn: "id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_items_departments_department_id",
                schema: "erp",
                table: "items");

            migrationBuilder.DropIndex(
                name: "IX_items_department_id",
                schema: "erp",
                table: "items");

            migrationBuilder.DropColumn(
                name: "BatchNumber",
                schema: "erp",
                table: "production_order_details");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                schema: "erp",
                table: "production_order_details");

            migrationBuilder.DropColumn(
                name: "Unit",
                schema: "erp",
                table: "production_order_details");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                schema: "erp",
                table: "production_order_details");

            migrationBuilder.DropColumn(
                name: "department_id",
                schema: "erp",
                table: "items");

            migrationBuilder.AlterColumn<long>(
                name: "recipe_id",
                schema: "erp",
                table: "production_order_details",
                type: "bigint",
                nullable: false,
                defaultValue: 0L,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldNullable: true);
        }
    }
}
