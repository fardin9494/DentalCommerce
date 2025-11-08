using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Catalog.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitBrands : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Country",
                schema: "catalog",
                columns: table => new
                {
                    Code2 = table.Column<string>(type: "nchar(2)", fixedLength: true, maxLength: 2, nullable: false),
                    Code3 = table.Column<string>(type: "nchar(3)", fixedLength: true, maxLength: 3, nullable: false),
                    NameFa = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    NameEn = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Region = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    FlagEmoji = table.Column<string>(type: "nvarchar(8)", maxLength: 8, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Country", x => x.Code2);
                });

            migrationBuilder.CreateTable(
                name: "Brand",
                schema: "catalog",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    NormalizedName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    CountryCode = table.Column<string>(type: "nchar(2)", fixedLength: true, maxLength: 2, nullable: false),
                    LogoMediaId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    EstablishedYear = table.Column<int>(type: "int", nullable: true),
                    Website = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Brand", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Brand_Country_CountryCode",
                        column: x => x.CountryCode,
                        principalSchema: "catalog",
                        principalTable: "Country",
                        principalColumn: "Code2",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BrandAlias",
                schema: "catalog",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BrandId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Alias = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Locale = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BrandAlias", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BrandAlias_Brand_BrandId",
                        column: x => x.BrandId,
                        principalSchema: "catalog",
                        principalTable: "Brand",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                schema: "catalog",
                table: "Country",
                columns: new[] { "Code2", "Code3", "FlagEmoji", "NameEn", "NameFa", "Region" },
                values: new object[,]
                {
                    { "CN", "CHN", "🇨🇳", "China", "چین", "Asia" },
                    { "DE", "DEU", "🇩🇪", "Germany", "آلمان", "Europe" },
                    { "IR", "IRN", "🇮🇷", "Iran", "ایران", "Asia" },
                    { "IT", "ITA", "🇮🇹", "Italy", "ایتالیا", "Europe" },
                    { "JP", "JPN", "🇯🇵", "Japan", "ژاپن", "Asia" },
                    { "TR", "TUR", "🇹🇷", "Türkiye", "ترکیه", "Asia" },
                    { "US", "USA", "🇺🇸", "United States", "آمریکا", "Americas" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Brand_CountryCode",
                schema: "catalog",
                table: "Brand",
                column: "CountryCode");

            migrationBuilder.CreateIndex(
                name: "IX_Brand_NormalizedName",
                schema: "catalog",
                table: "Brand",
                column: "NormalizedName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BrandAlias_BrandId_Alias",
                schema: "catalog",
                table: "BrandAlias",
                columns: new[] { "BrandId", "Alias" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Country_Code3",
                schema: "catalog",
                table: "Country",
                column: "Code3",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Country_NameEn",
                schema: "catalog",
                table: "Country",
                column: "NameEn");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BrandAlias",
                schema: "catalog");

            migrationBuilder.DropTable(
                name: "Brand",
                schema: "catalog");

            migrationBuilder.DropTable(
                name: "Country",
                schema: "catalog");
        }
    }
}
