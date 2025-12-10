using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Inventory.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddReceiptLineApprovalFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "ApprovedQty",
                schema: "inv",
                table: "ReceiptLine",
                type: "decimal(18,3)",
                precision: 18,
                scale: 3,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "RejectedQty",
                schema: "inv",
                table: "ReceiptLine",
                type: "decimal(18,3)",
                precision: 18,
                scale: 3,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "RejectionReason",
                schema: "inv",
                table: "ReceiptLine",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ApprovedQty",
                schema: "inv",
                table: "ReceiptLine");

            migrationBuilder.DropColumn(
                name: "RejectedQty",
                schema: "inv",
                table: "ReceiptLine");

            migrationBuilder.DropColumn(
                name: "RejectionReason",
                schema: "inv",
                table: "ReceiptLine");
        }
    }
}
