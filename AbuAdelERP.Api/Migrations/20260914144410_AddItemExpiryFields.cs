using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AbuAdelERP.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddItemExpiryFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "has_expiry_date",
                schema: "erp",
                table: "items",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "shelf_life_days",
                schema: "erp",
                table: "items",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "has_expiry_date",
                schema: "erp",
                table: "items");

            migrationBuilder.DropColumn(
                name: "shelf_life_days",
                schema: "erp",
                table: "items");
        }
    }
}
