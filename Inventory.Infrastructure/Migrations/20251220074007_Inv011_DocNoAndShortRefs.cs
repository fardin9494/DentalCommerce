using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Inventory.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Inv011_DocNoAndShortRefs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateSequence<long>(
                name: "ReceiptDocNoSeq",
                schema: "inv");

            migrationBuilder.CreateSequence<long>(
                name: "IssueDocNoSeq",
                schema: "inv");

            migrationBuilder.CreateSequence<long>(
                name: "TransferDocNoSeq",
                schema: "inv");

            migrationBuilder.CreateSequence<long>(
                name: "AdjustmentDocNoSeq",
                schema: "inv");

            migrationBuilder.AddColumn<long>(
                name: "DocNo",
                schema: "inv",
                table: "Transfer",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "DocNo",
                schema: "inv",
                table: "Receipt",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "DocNo",
                schema: "inv",
                table: "Issue",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "DocNo",
                schema: "inv",
                table: "Adjustment",
                type: "bigint",
                nullable: true);

            // Backfill DocNo for existing rows (ensures uniqueness before creating unique indexes)
            migrationBuilder.Sql("""
UPDATE inv.Transfer SET DocNo = NEXT VALUE FOR inv.TransferDocNoSeq WHERE DocNo IS NULL;
UPDATE inv.Receipt SET DocNo = NEXT VALUE FOR inv.ReceiptDocNoSeq WHERE DocNo IS NULL;
UPDATE inv.Issue SET DocNo = NEXT VALUE FOR inv.IssueDocNoSeq WHERE DocNo IS NULL;
UPDATE inv.Adjustment SET DocNo = NEXT VALUE FOR inv.AdjustmentDocNoSeq WHERE DocNo IS NULL;
""");

            migrationBuilder.AlterColumn<long>(
                name: "DocNo",
                schema: "inv",
                table: "Transfer",
                type: "bigint",
                nullable: false,
                defaultValueSql: "NEXT VALUE FOR inv.TransferDocNoSeq",
                oldClrType: typeof(long),
                oldType: "bigint",
                oldNullable: true);

            migrationBuilder.AlterColumn<long>(
                name: "DocNo",
                schema: "inv",
                table: "Receipt",
                type: "bigint",
                nullable: false,
                defaultValueSql: "NEXT VALUE FOR inv.ReceiptDocNoSeq",
                oldClrType: typeof(long),
                oldType: "bigint",
                oldNullable: true);

            migrationBuilder.AlterColumn<long>(
                name: "DocNo",
                schema: "inv",
                table: "Issue",
                type: "bigint",
                nullable: false,
                defaultValueSql: "NEXT VALUE FOR inv.IssueDocNoSeq",
                oldClrType: typeof(long),
                oldType: "bigint",
                oldNullable: true);

            migrationBuilder.AlterColumn<long>(
                name: "DocNo",
                schema: "inv",
                table: "Adjustment",
                type: "bigint",
                nullable: false,
                defaultValueSql: "NEXT VALUE FOR inv.AdjustmentDocNoSeq",
                oldClrType: typeof(long),
                oldType: "bigint",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Transfer_DocNo",
                schema: "inv",
                table: "Transfer",
                column: "DocNo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Receipt_DocNo",
                schema: "inv",
                table: "Receipt",
                column: "DocNo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Issue_DocNo",
                schema: "inv",
                table: "Issue",
                column: "DocNo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Adjustment_DocNo",
                schema: "inv",
                table: "Adjustment",
                column: "DocNo",
                unique: true);

            // Backfill user-facing references for existing rows (only when empty)
            migrationBuilder.Sql("""
UPDATE inv.Receipt
SET ExternalRef =
    'RCPT-' +
    CASE Reason
        WHEN 1 THEN 'PUR'
        WHEN 2 THEN 'RETIN'
        WHEN 3 THEN 'PRD'
        ELSE 'OTH'
    END +
    '-' +
    CONVERT(char(6), DocDate, 12) +
    '-' +
    RIGHT('000000' + CAST(DocNo AS varchar(20)), 6)
WHERE ExternalRef IS NULL OR LTRIM(RTRIM(ExternalRef)) = '';

UPDATE inv.Issue
SET ExternalRef =
    'ISS-' +
    CONVERT(char(6), DocDate, 12) +
    '-' +
    RIGHT('000000' + CAST(DocNo AS varchar(20)), 6)
WHERE ExternalRef IS NULL OR LTRIM(RTRIM(ExternalRef)) = '';

UPDATE inv.Transfer
SET ExternalRef =
    'TRF-' +
    CONVERT(char(6), DocDate, 12) +
    '-' +
    RIGHT('000000' + CAST(DocNo AS varchar(20)), 6)
WHERE ExternalRef IS NULL OR LTRIM(RTRIM(ExternalRef)) = '';

UPDATE inv.Adjustment
SET Note =
    'ADJ-' +
    CASE Reason
        WHEN 1 THEN 'INIT'
        WHEN 2 THEN 'DMG'
        WHEN 3 THEN 'EXP'
        WHEN 4 THEN 'FND'
        WHEN 5 THEN 'SHR'
        WHEN 6 THEN 'COR'
        ELSE 'OTH'
    END +
    '-' +
    CONVERT(char(6), DocDate, 12) +
    '-' +
    RIGHT('000000' + CAST(DocNo AS varchar(20)), 6)
WHERE Note IS NULL OR LTRIM(RTRIM(Note)) = '';
""");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Transfer_DocNo",
                schema: "inv",
                table: "Transfer");

            migrationBuilder.DropIndex(
                name: "IX_Receipt_DocNo",
                schema: "inv",
                table: "Receipt");

            migrationBuilder.DropIndex(
                name: "IX_Issue_DocNo",
                schema: "inv",
                table: "Issue");

            migrationBuilder.DropIndex(
                name: "IX_Adjustment_DocNo",
                schema: "inv",
                table: "Adjustment");

            migrationBuilder.DropColumn(
                name: "DocNo",
                schema: "inv",
                table: "Transfer");

            migrationBuilder.DropColumn(
                name: "DocNo",
                schema: "inv",
                table: "Receipt");

            migrationBuilder.DropColumn(
                name: "DocNo",
                schema: "inv",
                table: "Issue");

            migrationBuilder.DropColumn(
                name: "DocNo",
                schema: "inv",
                table: "Adjustment");

            migrationBuilder.DropSequence(
                name: "ReceiptDocNoSeq",
                schema: "inv");

            migrationBuilder.DropSequence(
                name: "IssueDocNoSeq",
                schema: "inv");

            migrationBuilder.DropSequence(
                name: "TransferDocNoSeq",
                schema: "inv");

            migrationBuilder.DropSequence(
                name: "AdjustmentDocNoSeq",
                schema: "inv");
        }
    }
}
