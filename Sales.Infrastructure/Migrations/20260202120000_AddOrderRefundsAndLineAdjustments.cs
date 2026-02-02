using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sales.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderRefundsAndLineAdjustments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CancelledQty",
                schema: "sales",
                table: "OrderLine",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "RefundedQty",
                schema: "sales",
                table: "OrderLine",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ReturnedQty",
                schema: "sales",
                table: "OrderLine",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "OrderRefund",
                schema: "sales",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Destination = table.Column<int>(type: "int", nullable: false),
                    Currency = table.Column<string>(type: "nvarchar(8)", maxLength: 8, nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    Note = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    RequestedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    RequestedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ApprovedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RejectedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RejectionReason = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    WalletReference = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderRefund", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OrderRefundLine",
                schema: "sales",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RefundId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrderLineId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SkuId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    BatchId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    UnitAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    LineAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderRefundLine", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrderRefundLine_OrderRefund_RefundId",
                        column: x => x.RefundId,
                        principalSchema: "sales",
                        principalTable: "OrderRefund",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OrderRefund_OrderId",
                schema: "sales",
                table: "OrderRefund",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderRefund_Status",
                schema: "sales",
                table: "OrderRefund",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_OrderRefundLine_OrderLineId",
                schema: "sales",
                table: "OrderRefundLine",
                column: "OrderLineId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderRefundLine_RefundId",
                schema: "sales",
                table: "OrderRefundLine",
                column: "RefundId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OrderRefundLine",
                schema: "sales");

            migrationBuilder.DropTable(
                name: "OrderRefund",
                schema: "sales");

            migrationBuilder.DropColumn(
                name: "CancelledQty",
                schema: "sales",
                table: "OrderLine");

            migrationBuilder.DropColumn(
                name: "RefundedQty",
                schema: "sales",
                table: "OrderLine");

            migrationBuilder.DropColumn(
                name: "ReturnedQty",
                schema: "sales",
                table: "OrderLine");
        }
    }
}
