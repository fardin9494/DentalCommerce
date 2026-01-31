using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pricing.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class EnsurePricingRowVersionPresent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                -- Re-create RowVersion if it was dropped or had the wrong type
                IF COL_LENGTH('pricing.PriceList', 'RowVersion') IS NULL
                BEGIN
                    ALTER TABLE pricing.PriceList ADD RowVersion rowversion NOT NULL;
                END
                ELSE
                BEGIN
                    DECLARE @colType nvarchar(128);
                    SELECT @colType = DATA_TYPE
                    FROM INFORMATION_SCHEMA.COLUMNS
                    WHERE TABLE_SCHEMA = 'pricing' AND TABLE_NAME = 'PriceList' AND COLUMN_NAME = 'RowVersion';

                    IF (@colType NOT IN ('rowversion', 'timestamp'))
                    BEGIN
                        ALTER TABLE pricing.PriceList DROP COLUMN RowVersion;
                        ALTER TABLE pricing.PriceList ADD RowVersion rowversion NOT NULL;
                    END
                END;

                IF COL_LENGTH('pricing.PriceOverride', 'RowVersion') IS NULL
                BEGIN
                    ALTER TABLE pricing.PriceOverride ADD RowVersion rowversion NOT NULL;
                END
                ELSE
                BEGIN
                    DECLARE @colType2 nvarchar(128);
                    SELECT @colType2 = DATA_TYPE
                    FROM INFORMATION_SCHEMA.COLUMNS
                    WHERE TABLE_SCHEMA = 'pricing' AND TABLE_NAME = 'PriceOverride' AND COLUMN_NAME = 'RowVersion';

                    IF (@colType2 NOT IN ('rowversion', 'timestamp'))
                    BEGIN
                        ALTER TABLE pricing.PriceOverride DROP COLUMN RowVersion;
                        ALTER TABLE pricing.PriceOverride ADD RowVersion rowversion NOT NULL;
                    END
                END;

                IF COL_LENGTH('pricing.PromotionCampaign', 'RowVersion') IS NULL
                BEGIN
                    ALTER TABLE pricing.PromotionCampaign ADD RowVersion rowversion NOT NULL;
                END
                ELSE
                BEGIN
                    DECLARE @colType3 nvarchar(128);
                    SELECT @colType3 = DATA_TYPE
                    FROM INFORMATION_SCHEMA.COLUMNS
                    WHERE TABLE_SCHEMA = 'pricing' AND TABLE_NAME = 'PromotionCampaign' AND COLUMN_NAME = 'RowVersion';

                    IF (@colType3 NOT IN ('rowversion', 'timestamp'))
                    BEGIN
                        ALTER TABLE pricing.PromotionCampaign DROP COLUMN RowVersion;
                        ALTER TABLE pricing.PromotionCampaign ADD RowVersion rowversion NOT NULL;
                    END
                END;

                IF COL_LENGTH('pricing.Coupon', 'RowVersion') IS NULL
                BEGIN
                    ALTER TABLE pricing.Coupon ADD RowVersion rowversion NOT NULL;
                END
                ELSE
                BEGIN
                    DECLARE @colType4 nvarchar(128);
                    SELECT @colType4 = DATA_TYPE
                    FROM INFORMATION_SCHEMA.COLUMNS
                    WHERE TABLE_SCHEMA = 'pricing' AND TABLE_NAME = 'Coupon' AND COLUMN_NAME = 'RowVersion';

                    IF (@colType4 NOT IN ('rowversion', 'timestamp'))
                    BEGIN
                        ALTER TABLE pricing.Coupon DROP COLUMN RowVersion;
                        ALTER TABLE pricing.Coupon ADD RowVersion rowversion NOT NULL;
                    END
                END;

                IF COL_LENGTH('pricing.PricingPolicy', 'RowVersion') IS NULL
                BEGIN
                    ALTER TABLE pricing.PricingPolicy ADD RowVersion rowversion NOT NULL;
                END
                ELSE
                BEGIN
                    DECLARE @colType5 nvarchar(128);
                    SELECT @colType5 = DATA_TYPE
                    FROM INFORMATION_SCHEMA.COLUMNS
                    WHERE TABLE_SCHEMA = 'pricing' AND TABLE_NAME = 'PricingPolicy' AND COLUMN_NAME = 'RowVersion';

                    IF (@colType5 NOT IN ('rowversion', 'timestamp'))
                    BEGIN
                        ALTER TABLE pricing.PricingPolicy DROP COLUMN RowVersion;
                        ALTER TABLE pricing.PricingPolicy ADD RowVersion rowversion NOT NULL;
                    END
                END;

                -- Force RowVersion refresh for existing rows
                UPDATE pricing.PriceList SET UpdatedAt = SYSUTCDATETIME();
                UPDATE pricing.PriceOverride SET UpdatedAt = SYSUTCDATETIME();
                UPDATE pricing.PromotionCampaign SET UpdatedAt = SYSUTCDATETIME();
                UPDATE pricing.Coupon SET UpdatedAt = SYSUTCDATETIME();
                UPDATE pricing.PricingPolicy SET UpdatedAt = SYSUTCDATETIME();
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Keep RowVersion intact even when rolling back to avoid losing concurrency tracking.
        }
    }
}
