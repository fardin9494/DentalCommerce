using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Inventory.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSkuToStockItem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Sku",
                schema: "inv",
                table: "StockItem",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_StockItem_Sku",
                schema: "inv",
                table: "StockItem",
                column: "Sku");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_StockItem_Sku",
                schema: "inv",
                table: "StockItem");

            migrationBuilder.DropColumn(
                name: "Sku",
                schema: "inv",
                table: "StockItem");
        }
    }
}
