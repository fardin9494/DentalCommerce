using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sales.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class EnsureOrderRowVersionPresent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                -- Re-create RowVersion if it was dropped or had the wrong type
                IF COL_LENGTH('sales.[Order]', 'RowVersion') IS NULL
                BEGIN
                    ALTER TABLE sales.[Order] ADD RowVersion rowversion NOT NULL;
                END
                ELSE
                BEGIN
                    DECLARE @colType nvarchar(128);
                    SELECT @colType = DATA_TYPE
                    FROM INFORMATION_SCHEMA.COLUMNS
                    WHERE TABLE_SCHEMA = 'sales' AND TABLE_NAME = 'Order' AND COLUMN_NAME = 'RowVersion';

                    IF (@colType NOT IN ('rowversion', 'timestamp'))
                    BEGIN
                        ALTER TABLE sales.[Order] DROP COLUMN RowVersion;
                        ALTER TABLE sales.[Order] ADD RowVersion rowversion NOT NULL;
                    END
                END;

                -- Force RowVersion refresh for existing rows
                UPDATE sales.[Order]
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
