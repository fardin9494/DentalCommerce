using System;
using Inventory.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Inventory.Infrastructure.Migrations
{
    [DbContext(typeof(InventoryDbContext))]
    [Migration("20251222160000_AddReceiptRejectionTracking")]
    public partial class AddReceiptRejectionTracking : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "RejectionStatus",
                schema: "inv",
                table: "ReceiptLine",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "RejectionResolvedAt",
                schema: "inv",
                table: "ReceiptLine",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RejectionResolutionNote",
                schema: "inv",
                table: "ReceiptLine",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.Sql("""
UPDATE inv.ReceiptLine
SET RejectionStatus = 1
WHERE RejectedQty > 0 AND RejectionStatus = 0;
""");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RejectionResolvedAt",
                schema: "inv",
                table: "ReceiptLine");

            migrationBuilder.DropColumn(
                name: "RejectionResolutionNote",
                schema: "inv",
                table: "ReceiptLine");

            migrationBuilder.DropColumn(
                name: "RejectionStatus",
                schema: "inv",
                table: "ReceiptLine");
        }
    }
}
