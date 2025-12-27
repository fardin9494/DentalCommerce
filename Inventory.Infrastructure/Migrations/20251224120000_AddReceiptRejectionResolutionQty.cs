using Inventory.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Inventory.Infrastructure.Migrations
{
    [DbContext(typeof(InventoryDbContext))]
    [Migration("20251224120000_AddReceiptRejectionResolutionQty")]
    public partial class AddReceiptRejectionResolutionQty : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "RejectionApprovedQty",
                schema: "inv",
                table: "ReceiptLine",
                type: "decimal(18,3)",
                precision: 18,
                scale: 3,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "RejectionReturnedQty",
                schema: "inv",
                table: "ReceiptLine",
                type: "decimal(18,3)",
                precision: 18,
                scale: 3,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "RejectionDisposedQty",
                schema: "inv",
                table: "ReceiptLine",
                type: "decimal(18,3)",
                precision: 18,
                scale: 3,
                nullable: false,
                defaultValue: 0m);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RejectionApprovedQty",
                schema: "inv",
                table: "ReceiptLine");

            migrationBuilder.DropColumn(
                name: "RejectionReturnedQty",
                schema: "inv",
                table: "ReceiptLine");

            migrationBuilder.DropColumn(
                name: "RejectionDisposedQty",
                schema: "inv",
                table: "ReceiptLine");
        }
    }
}
