using System;
using Inventory.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Inventory.Infrastructure.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(InventoryDbContext))]
    [Migration("20251215140000_AddStockItemIdToAdjustmentLine")]
    public partial class AddStockItemIdToAdjustmentLine : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "StockItemId",
                schema: "inv",
                table: "AdjustmentLine",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.Sql("""
-- 1) Try exact match on lot/expiry
WITH cte_exact AS (
    SELECT 
        al.Id AS AdjustmentLineId,
        si.Id AS StockItemId,
        ROW_NUMBER() OVER (
            PARTITION BY al.Id
            ORDER BY si.UpdatedAt DESC, si.CreatedAt DESC
        ) AS rn
    FROM inv.AdjustmentLine al
    INNER JOIN inv.Adjustment a ON a.Id = al.AdjustmentId
    INNER JOIN inv.StockItem si ON si.ProductId = al.ProductId
        AND ((si.VariantId IS NULL AND al.VariantId IS NULL) OR si.VariantId = al.VariantId)
        AND si.WarehouseId = a.WarehouseId
        AND ((si.LotNumber IS NULL AND al.LotNumber IS NULL) OR si.LotNumber = al.LotNumber)
        AND ((si.ExpiryDate IS NULL AND al.ExpiryDate IS NULL) OR si.ExpiryDate = al.ExpiryDate)
)
UPDATE al
SET StockItemId = c.StockItemId
FROM inv.AdjustmentLine al
INNER JOIN cte_exact c ON c.AdjustmentLineId = al.Id AND c.rn = 1
WHERE al.StockItemId IS NULL;

-- 2) Fallback: ignore lot/expiry, pick latest stock item for the same product/variant/warehouse
UPDATE al
SET StockItemId = si.Id
FROM inv.AdjustmentLine al
INNER JOIN inv.Adjustment a ON a.Id = al.AdjustmentId
CROSS APPLY (
    SELECT TOP (1) s.Id
    FROM inv.StockItem s
    WHERE s.ProductId = al.ProductId
      AND ((s.VariantId IS NULL AND al.VariantId IS NULL) OR s.VariantId = al.VariantId)
      AND s.WarehouseId = a.WarehouseId
    ORDER BY s.UpdatedAt DESC, s.CreatedAt DESC
) si
WHERE al.StockItemId IS NULL;
""");

            migrationBuilder.AlterColumn<Guid>(
                name: "StockItemId",
                schema: "inv",
                table: "AdjustmentLine",
                type: "uniqueidentifier",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_AdjustmentLine_StockItemId",
                schema: "inv",
                table: "AdjustmentLine",
                column: "StockItemId");

            migrationBuilder.AddForeignKey(
                name: "FK_AdjustmentLine_StockItem_StockItemId",
                schema: "inv",
                table: "AdjustmentLine",
                column: "StockItemId",
                principalSchema: "inv",
                principalTable: "StockItem",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AdjustmentLine_StockItem_StockItemId",
                schema: "inv",
                table: "AdjustmentLine");

            migrationBuilder.DropIndex(
                name: "IX_AdjustmentLine_StockItemId",
                schema: "inv",
                table: "AdjustmentLine");

            migrationBuilder.DropColumn(
                name: "StockItemId",
                schema: "inv",
                table: "AdjustmentLine");
        }
    }
}
