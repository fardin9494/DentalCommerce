using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sales.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderNumberToOrder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "OrderNumber",
                schema: "sales",
                table: "Order",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: false,
                defaultValueSql: "('SO-' + CONVERT(varchar(8), GETUTCDATE(), 112) + '-' + REPLACE(CONVERT(varchar(8), GETUTCDATE(), 108), ':', '') + '-' + RIGHT(CONVERT(varchar(36), NEWID()), 4))");

            migrationBuilder.CreateIndex(
                name: "IX_Order_OrderNumber",
                schema: "sales",
                table: "Order",
                column: "OrderNumber",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Order_OrderNumber",
                schema: "sales",
                table: "Order");

            migrationBuilder.DropColumn(
                name: "OrderNumber",
                schema: "sales",
                table: "Order");
        }
    }
}
