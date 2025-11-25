using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Inventory.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Inv003_StockCore : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "StockItem",
                schema: "inv",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VariantId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    WarehouseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LotNumber = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    ExpiryDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    OnHand = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    Reserved = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    Blocked = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    BlockReason = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockItem", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "StockLedger",
                schema: "inv",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VariantId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    WarehouseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LotNumber = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    ExpiryDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeltaQty = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    UnitCost = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    MovementType = table.Column<int>(type: "int", nullable: false),
                    RefDocType = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    RefDocId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Note = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockLedger", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_StockItem_ProductId_VariantId",
                schema: "inv",
                table: "StockItem",
                columns: new[] { "ProductId", "VariantId" });

            migrationBuilder.CreateIndex(
                name: "IX_StockItem_ProductId_VariantId_WarehouseId_LotNumber_ExpiryDate",
                schema: "inv",
                table: "StockItem",
                columns: new[] { "ProductId", "VariantId", "WarehouseId", "LotNumber", "ExpiryDate" },
                unique: true,
                filter: "[VariantId] IS NOT NULL AND [LotNumber] IS NOT NULL AND [ExpiryDate] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_StockItem_WarehouseId_ExpiryDate",
                schema: "inv",
                table: "StockItem",
                columns: new[] { "WarehouseId", "ExpiryDate" });

            migrationBuilder.CreateIndex(
                name: "IX_StockLedger_ProductId_VariantId_WarehouseId_Timestamp",
                schema: "inv",
                table: "StockLedger",
                columns: new[] { "ProductId", "VariantId", "WarehouseId", "Timestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_StockLedger_WarehouseId_LotNumber_ExpiryDate",
                schema: "inv",
                table: "StockLedger",
                columns: new[] { "WarehouseId", "LotNumber", "ExpiryDate" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StockItem",
                schema: "inv");

            migrationBuilder.DropTable(
                name: "StockLedger",
                schema: "inv");
        }
    }
}
