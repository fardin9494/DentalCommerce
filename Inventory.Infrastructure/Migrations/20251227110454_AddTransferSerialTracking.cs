using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Inventory.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTransferSerialTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "TransferId",
                schema: "inv",
                table: "StockItemSerial",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "TransferLineId",
                schema: "inv",
                table: "StockItemSerial",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "TransferSegmentId",
                schema: "inv",
                table: "StockItemSerial",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_StockItemSerial_TransferId",
                schema: "inv",
                table: "StockItemSerial",
                column: "TransferId");

            migrationBuilder.CreateIndex(
                name: "IX_StockItemSerial_TransferLineId",
                schema: "inv",
                table: "StockItemSerial",
                column: "TransferLineId");

            migrationBuilder.CreateIndex(
                name: "IX_StockItemSerial_TransferSegmentId",
                schema: "inv",
                table: "StockItemSerial",
                column: "TransferSegmentId");

            migrationBuilder.AddForeignKey(
                name: "FK_StockItemSerial_TransferLine_TransferLineId",
                schema: "inv",
                table: "StockItemSerial",
                column: "TransferLineId",
                principalSchema: "inv",
                principalTable: "TransferLine",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_StockItemSerial_TransferSegment_TransferSegmentId",
                schema: "inv",
                table: "StockItemSerial",
                column: "TransferSegmentId",
                principalSchema: "inv",
                principalTable: "TransferSegment",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_StockItemSerial_Transfer_TransferId",
                schema: "inv",
                table: "StockItemSerial",
                column: "TransferId",
                principalSchema: "inv",
                principalTable: "Transfer",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_StockItemSerial_TransferLine_TransferLineId",
                schema: "inv",
                table: "StockItemSerial");

            migrationBuilder.DropForeignKey(
                name: "FK_StockItemSerial_TransferSegment_TransferSegmentId",
                schema: "inv",
                table: "StockItemSerial");

            migrationBuilder.DropForeignKey(
                name: "FK_StockItemSerial_Transfer_TransferId",
                schema: "inv",
                table: "StockItemSerial");

            migrationBuilder.DropIndex(
                name: "IX_StockItemSerial_TransferId",
                schema: "inv",
                table: "StockItemSerial");

            migrationBuilder.DropIndex(
                name: "IX_StockItemSerial_TransferLineId",
                schema: "inv",
                table: "StockItemSerial");

            migrationBuilder.DropIndex(
                name: "IX_StockItemSerial_TransferSegmentId",
                schema: "inv",
                table: "StockItemSerial");

            migrationBuilder.DropColumn(
                name: "TransferId",
                schema: "inv",
                table: "StockItemSerial");

            migrationBuilder.DropColumn(
                name: "TransferLineId",
                schema: "inv",
                table: "StockItemSerial");

            migrationBuilder.DropColumn(
                name: "TransferSegmentId",
                schema: "inv",
                table: "StockItemSerial");
        }
    }
}
