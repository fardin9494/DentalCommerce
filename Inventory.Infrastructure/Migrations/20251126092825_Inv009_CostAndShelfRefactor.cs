using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Inventory.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Inv009_CostAndShelfRefactor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StockItemPrice",
                schema: "inv");

            migrationBuilder.DropIndex(
                name: "IX_StockItem_ProductId_VariantId",
                schema: "inv",
                table: "StockItem");

            migrationBuilder.DropIndex(
                name: "IX_StockItem_ProductId_VariantId_WarehouseId_LotNumber_ExpiryDate",
                schema: "inv",
                table: "StockItem");

            migrationBuilder.DropIndex(
                name: "IX_StockItem_WarehouseId_ExpiryDate",
                schema: "inv",
                table: "StockItem");

            migrationBuilder.RenameColumn(
                name: "PostedAt",
                schema: "inv",
                table: "Receipt",
                newName: "ReceivedAt");

            migrationBuilder.AlterColumn<string>(
                name: "BlockReason",
                schema: "inv",
                table: "StockItem",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(256)",
                oldMaxLength: 256,
                oldNullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                schema: "inv",
                table: "StockItem",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<Guid>(
                name: "ShelfId",
                schema: "inv",
                table: "StockItem",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ApprovedAt",
                schema: "inv",
                table: "Receipt",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Reason",
                schema: "inv",
                table: "Receipt",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "InventoryCosts",
                schema: "inv",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StockItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    Currency = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    RecordedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InventoryCosts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "StockShelves",
                schema: "inv",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WarehouseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockShelves", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_StockItem_ProductId_VariantId_WarehouseId_LotNumber_ExpiryDate_ShelfId",
                schema: "inv",
                table: "StockItem",
                columns: new[] { "ProductId", "VariantId", "WarehouseId", "LotNumber", "ExpiryDate", "ShelfId" },
                unique: true,
                filter: "[VariantId] IS NOT NULL AND [LotNumber] IS NOT NULL AND [ExpiryDate] IS NOT NULL AND [ShelfId] IS NOT NULL");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Stock_Blocked_NonNeg",
                schema: "inv",
                table: "StockItem",
                sql: "[Blocked] >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Stock_OnHand_NonNeg",
                schema: "inv",
                table: "StockItem",
                sql: "[OnHand] >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Stock_Reserved_NonNeg",
                schema: "inv",
                table: "StockItem",
                sql: "[Reserved] >= 0");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryCosts_StockItemId",
                schema: "inv",
                table: "InventoryCosts",
                column: "StockItemId");

            migrationBuilder.CreateIndex(
                name: "IX_StockShelves_WarehouseId_Name",
                schema: "inv",
                table: "StockShelves",
                columns: new[] { "WarehouseId", "Name" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InventoryCosts",
                schema: "inv");

            migrationBuilder.DropTable(
                name: "StockShelves",
                schema: "inv");

            migrationBuilder.DropIndex(
                name: "IX_StockItem_ProductId_VariantId_WarehouseId_LotNumber_ExpiryDate_ShelfId",
                schema: "inv",
                table: "StockItem");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Stock_Blocked_NonNeg",
                schema: "inv",
                table: "StockItem");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Stock_OnHand_NonNeg",
                schema: "inv",
                table: "StockItem");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Stock_Reserved_NonNeg",
                schema: "inv",
                table: "StockItem");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                schema: "inv",
                table: "StockItem");

            migrationBuilder.DropColumn(
                name: "ShelfId",
                schema: "inv",
                table: "StockItem");

            migrationBuilder.DropColumn(
                name: "ApprovedAt",
                schema: "inv",
                table: "Receipt");

            migrationBuilder.DropColumn(
                name: "Reason",
                schema: "inv",
                table: "Receipt");

            migrationBuilder.RenameColumn(
                name: "ReceivedAt",
                schema: "inv",
                table: "Receipt",
                newName: "PostedAt");

            migrationBuilder.AlterColumn<string>(
                name: "BlockReason",
                schema: "inv",
                table: "StockItem",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.CreateTable(
                name: "StockItemPrice",
                schema: "inv",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Currency = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    EffectiveFrom = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EffectiveTo = table.Column<DateTime>(type: "datetime2", nullable: true),
                    StockItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockItemPrice", x => x.Id);
                    table.CheckConstraint("CK_StockItemPrice_Positive", "[Amount] > 0");
                    table.CheckConstraint("CK_StockItemPrice_Range", "[EffectiveTo] IS NULL OR [EffectiveTo] >= [EffectiveFrom]");
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
                name: "IX_StockItemPrice_StockItemId",
                schema: "inv",
                table: "StockItemPrice",
                column: "StockItemId");

            migrationBuilder.CreateIndex(
                name: "IX_StockItemPrice_StockItemId_EffectiveFrom",
                schema: "inv",
                table: "StockItemPrice",
                columns: new[] { "StockItemId", "EffectiveFrom" },
                unique: true);
        }
    }
}
