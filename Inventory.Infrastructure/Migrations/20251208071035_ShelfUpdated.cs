using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Inventory.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ShelfUpdated : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Code",
                schema: "inv",
                table: "StockShelves",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "ColumnNumber",
                schema: "inv",
                table: "StockShelves",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "LevelNumber",
                schema: "inv",
                table: "StockShelves",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "RowNumber",
                schema: "inv",
                table: "StockShelves",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_StockShelves_WarehouseId_Code",
                schema: "inv",
                table: "StockShelves",
                columns: new[] { "WarehouseId", "Code" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_StockShelves_WarehouseId_Code",
                schema: "inv",
                table: "StockShelves");

            migrationBuilder.DropColumn(
                name: "Code",
                schema: "inv",
                table: "StockShelves");

            migrationBuilder.DropColumn(
                name: "ColumnNumber",
                schema: "inv",
                table: "StockShelves");

            migrationBuilder.DropColumn(
                name: "LevelNumber",
                schema: "inv",
                table: "StockShelves");

            migrationBuilder.DropColumn(
                name: "RowNumber",
                schema: "inv",
                table: "StockShelves");
        }
    }
}
