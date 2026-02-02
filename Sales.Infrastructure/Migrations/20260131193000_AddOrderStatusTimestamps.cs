using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sales.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderStatusTimestamps : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DeliveredAtUtc",
                schema: "sales",
                table: "Order",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "RefundedAtUtc",
                schema: "sales",
                table: "Order",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ReturnedAtUtc",
                schema: "sales",
                table: "Order",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ShippedAtUtc",
                schema: "sales",
                table: "Order",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DeliveredAtUtc",
                schema: "sales",
                table: "Order");

            migrationBuilder.DropColumn(
                name: "RefundedAtUtc",
                schema: "sales",
                table: "Order");

            migrationBuilder.DropColumn(
                name: "ReturnedAtUtc",
                schema: "sales",
                table: "Order");

            migrationBuilder.DropColumn(
                name: "ShippedAtUtc",
                schema: "sales",
                table: "Order");
        }
    }
}
