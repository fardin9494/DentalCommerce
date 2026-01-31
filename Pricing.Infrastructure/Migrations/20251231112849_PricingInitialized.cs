using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pricing.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class PricingInitialized : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "pricing");

            migrationBuilder.CreateTable(
                name: "Coupon",
                schema: "pricing",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    ValidFrom = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ValidTo = table.Column<DateTime>(type: "datetime2", nullable: true),
                    MaxUsesTotal = table.Column<int>(type: "int", nullable: true),
                    MaxUsesPerUser = table.Column<int>(type: "int", nullable: true),
                    Priority = table.Column<int>(type: "int", nullable: false),
                    CombinableWithPromotions = table.Column<bool>(type: "bit", nullable: false),
                    ExclusiveGroup = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    Guardrails = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Eligibility = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Benefit = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "varbinary(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Coupon", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CouponRedemption",
                schema: "pricing",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CouponId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CouponCode = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SiteId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CartHash = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    ReservationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RedeemedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    OrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CouponRedemption", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PriceList",
                schema: "pricing",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Currency = table.Column<string>(type: "nvarchar(8)", maxLength: 8, nullable: false),
                    ValidFrom = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ValidTo = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "varbinary(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PriceList", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PriceOverride",
                schema: "pricing",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ScopeType = table.Column<int>(type: "int", nullable: false),
                    ScopeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SkuId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    OverrideType = table.Column<int>(type: "int", nullable: false),
                    Value = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    Currency = table.Column<string>(type: "nvarchar(8)", maxLength: 8, nullable: false),
                    ValidFrom = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ValidTo = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Priority = table.Column<int>(type: "int", nullable: false),
                    StackingGroup = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "varbinary(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PriceOverride", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PricingPolicy",
                schema: "pricing",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SiteId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DefaultStackingMode = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "varbinary(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PricingPolicy", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PromotionCampaign",
                schema: "pricing",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    ValidFrom = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ValidTo = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Priority = table.Column<int>(type: "int", nullable: false),
                    StackingGroup = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    StackingMode = table.Column<int>(type: "int", nullable: false),
                    CombinableWithOtherPromotions = table.Column<bool>(type: "bit", nullable: false),
                    CombinableWithCoupons = table.Column<bool>(type: "bit", nullable: false),
                    ExclusiveGroup = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    Guardrails = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Eligibility = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Benefit = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "varbinary(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PromotionCampaign", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PriceListItem",
                schema: "pricing",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PriceListId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SkuId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    BasePrice = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    TierPrices = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PriceListItem", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PriceListItem_PriceList_PriceListId",
                        column: x => x.PriceListId,
                        principalSchema: "pricing",
                        principalTable: "PriceList",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Coupon_Code",
                schema: "pricing",
                table: "Coupon",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Coupon_IsActive_ValidFrom_ValidTo",
                schema: "pricing",
                table: "Coupon",
                columns: new[] { "IsActive", "ValidFrom", "ValidTo" });

            migrationBuilder.CreateIndex(
                name: "IX_CouponRedemption_CouponCode",
                schema: "pricing",
                table: "CouponRedemption",
                column: "CouponCode");

            migrationBuilder.CreateIndex(
                name: "IX_CouponRedemption_CouponId",
                schema: "pricing",
                table: "CouponRedemption",
                column: "CouponId");

            migrationBuilder.CreateIndex(
                name: "IX_CouponRedemption_ExpiresAt",
                schema: "pricing",
                table: "CouponRedemption",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_CouponRedemption_ReservationId",
                schema: "pricing",
                table: "CouponRedemption",
                column: "ReservationId");

            migrationBuilder.CreateIndex(
                name: "IX_CouponRedemption_UserId_CouponId",
                schema: "pricing",
                table: "CouponRedemption",
                columns: new[] { "UserId", "CouponId" });

            migrationBuilder.CreateIndex(
                name: "IX_PriceList_IsActive_ValidFrom_ValidTo",
                schema: "pricing",
                table: "PriceList",
                columns: new[] { "IsActive", "ValidFrom", "ValidTo" });

            migrationBuilder.CreateIndex(
                name: "IX_PriceListItem_PriceListId",
                schema: "pricing",
                table: "PriceListItem",
                column: "PriceListId");

            migrationBuilder.CreateIndex(
                name: "IX_PriceListItem_SkuId",
                schema: "pricing",
                table: "PriceListItem",
                column: "SkuId");

            migrationBuilder.CreateIndex(
                name: "IX_PriceOverride_ScopeType_ScopeId",
                schema: "pricing",
                table: "PriceOverride",
                columns: new[] { "ScopeType", "ScopeId" });

            migrationBuilder.CreateIndex(
                name: "IX_PriceOverride_SkuId",
                schema: "pricing",
                table: "PriceOverride",
                column: "SkuId");

            migrationBuilder.CreateIndex(
                name: "IX_PricingPolicy_SiteId",
                schema: "pricing",
                table: "PricingPolicy",
                column: "SiteId");

            migrationBuilder.CreateIndex(
                name: "IX_PromotionCampaign_IsActive_ValidFrom_ValidTo",
                schema: "pricing",
                table: "PromotionCampaign",
                columns: new[] { "IsActive", "ValidFrom", "ValidTo" });

            migrationBuilder.CreateIndex(
                name: "IX_PromotionCampaign_StackingGroup",
                schema: "pricing",
                table: "PromotionCampaign",
                column: "StackingGroup");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Coupon",
                schema: "pricing");

            migrationBuilder.DropTable(
                name: "CouponRedemption",
                schema: "pricing");

            migrationBuilder.DropTable(
                name: "PriceListItem",
                schema: "pricing");

            migrationBuilder.DropTable(
                name: "PriceOverride",
                schema: "pricing");

            migrationBuilder.DropTable(
                name: "PricingPolicy",
                schema: "pricing");

            migrationBuilder.DropTable(
                name: "PromotionCampaign",
                schema: "pricing");

            migrationBuilder.DropTable(
                name: "PriceList",
                schema: "pricing");
        }
    }
}
