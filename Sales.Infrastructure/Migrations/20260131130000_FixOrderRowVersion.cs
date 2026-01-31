using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sales.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FixOrderRowVersion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF COL_LENGTH('sales.Order', 'RowVersion') IS NULL
BEGIN
    ALTER TABLE [sales].[Order] ADD [RowVersion] rowversion NOT NULL;
END");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF COL_LENGTH('sales.Order', 'RowVersion') IS NOT NULL
BEGIN
    ALTER TABLE [sales].[Order] DROP COLUMN [RowVersion];
END");
        }
    }
}
