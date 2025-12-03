using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Inventory.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class EnsureStockItemRowVersionPresent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                -- Re-create RowVersion if it was dropped or had the wrong type
                IF COL_LENGTH('inv.StockItem', 'RowVersion') IS NULL
                BEGIN
                    ALTER TABLE inv.StockItem ADD RowVersion rowversion NOT NULL;
                END
                ELSE
                BEGIN
                    DECLARE @colType nvarchar(128);
                    SELECT @colType = DATA_TYPE
                    FROM INFORMATION_SCHEMA.COLUMNS
                    WHERE TABLE_SCHEMA = 'inv' AND TABLE_NAME = 'StockItem' AND COLUMN_NAME = 'RowVersion';

                    IF (@colType NOT IN ('rowversion', 'timestamp'))
                    BEGIN
                        ALTER TABLE inv.StockItem DROP COLUMN RowVersion;
                        ALTER TABLE inv.StockItem ADD RowVersion rowversion NOT NULL;
                    END
                END;

                -- Force RowVersion refresh for existing rows
                UPDATE inv.StockItem
                SET UpdatedAt = SYSUTCDATETIME();
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Keep RowVersion intact even when rolling back to avoid losing concurrency tracking.
        }
    }
}
