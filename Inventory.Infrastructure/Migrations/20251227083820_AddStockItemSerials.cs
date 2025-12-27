using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Inventory.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddStockItemSerials : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "StockItemSerial",
                schema: "inv",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReceiptLineId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StockItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SerialNumber = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    IssueId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IssueLineId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IssuedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockItemSerial", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StockItemSerial_IssueLine_IssueLineId",
                        column: x => x.IssueLineId,
                        principalSchema: "inv",
                        principalTable: "IssueLine",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.NoAction);
                    table.ForeignKey(
                        name: "FK_StockItemSerial_Issue_IssueId",
                        column: x => x.IssueId,
                        principalSchema: "inv",
                        principalTable: "Issue",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.NoAction);
                    table.ForeignKey(
                        name: "FK_StockItemSerial_ReceiptLine_ReceiptLineId",
                        column: x => x.ReceiptLineId,
                        principalSchema: "inv",
                        principalTable: "ReceiptLine",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_StockItemSerial_StockItem_StockItemId",
                        column: x => x.StockItemId,
                        principalSchema: "inv",
                        principalTable: "StockItem",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_StockItemSerial_IssueId",
                schema: "inv",
                table: "StockItemSerial",
                column: "IssueId");

            migrationBuilder.CreateIndex(
                name: "IX_StockItemSerial_IssueLineId",
                schema: "inv",
                table: "StockItemSerial",
                column: "IssueLineId");

            migrationBuilder.CreateIndex(
                name: "IX_StockItemSerial_ReceiptLineId",
                schema: "inv",
                table: "StockItemSerial",
                column: "ReceiptLineId");

            migrationBuilder.CreateIndex(
                name: "IX_StockItemSerial_SerialNumber",
                schema: "inv",
                table: "StockItemSerial",
                column: "SerialNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StockItemSerial_StockItemId",
                schema: "inv",
                table: "StockItemSerial",
                column: "StockItemId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StockItemSerial",
                schema: "inv");
        }
    }
}
