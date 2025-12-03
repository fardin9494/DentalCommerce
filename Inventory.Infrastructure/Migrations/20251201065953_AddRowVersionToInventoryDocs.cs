using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Inventory.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRowVersionToInventoryDocs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                schema: "inv",
                table: "Warehouse",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                schema: "inv",
                table: "TransferSegment",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                schema: "inv",
                table: "TransferLine",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                schema: "inv",
                table: "Transfer",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                schema: "inv",
                table: "StockShelves",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                schema: "inv",
                table: "StockLedger",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                schema: "inv",
                table: "ReceiptLine",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                schema: "inv",
                table: "Receipt",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                schema: "inv",
                table: "IssueLine",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                schema: "inv",
                table: "IssueAllocation",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                schema: "inv",
                table: "Issue",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                schema: "inv",
                table: "InventoryCosts",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                schema: "inv",
                table: "AdjustmentLine",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                schema: "inv",
                table: "Adjustment",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RowVersion",
                schema: "inv",
                table: "Warehouse");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                schema: "inv",
                table: "TransferSegment");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                schema: "inv",
                table: "TransferLine");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                schema: "inv",
                table: "Transfer");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                schema: "inv",
                table: "StockShelves");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                schema: "inv",
                table: "StockLedger");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                schema: "inv",
                table: "ReceiptLine");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                schema: "inv",
                table: "Receipt");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                schema: "inv",
                table: "IssueLine");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                schema: "inv",
                table: "IssueAllocation");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                schema: "inv",
                table: "Issue");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                schema: "inv",
                table: "InventoryCosts");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                schema: "inv",
                table: "AdjustmentLine");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                schema: "inv",
                table: "Adjustment");
        }
    }
}
