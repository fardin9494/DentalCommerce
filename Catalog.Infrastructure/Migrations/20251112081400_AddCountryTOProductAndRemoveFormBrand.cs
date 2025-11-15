using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Catalog.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCountryTOProductAndRemoveFormBrand : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Brand_Country_CountryCode",
                schema: "catalog",
                table: "Brand");

            migrationBuilder.DropIndex(
                name: "IX_Brand_CountryCode",
                schema: "catalog",
                table: "Brand");

            migrationBuilder.DropColumn(
                name: "CountryCode",
                schema: "catalog",
                table: "Brand");

            migrationBuilder.AddColumn<string>(
                name: "CountryCode",
                schema: "catalog",
                table: "Product",
                type: "nchar(2)",
                fixedLength: true,
                maxLength: 2,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Product_CountryCode",
                schema: "catalog",
                table: "Product",
                column: "CountryCode");

            migrationBuilder.AddForeignKey(
                name: "FK_Product_Country_CountryCode",
                schema: "catalog",
                table: "Product",
                column: "CountryCode",
                principalSchema: "catalog",
                principalTable: "Country",
                principalColumn: "Code2");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Product_Country_CountryCode",
                schema: "catalog",
                table: "Product");

            migrationBuilder.DropIndex(
                name: "IX_Product_CountryCode",
                schema: "catalog",
                table: "Product");

            migrationBuilder.DropColumn(
                name: "CountryCode",
                schema: "catalog",
                table: "Product");

            migrationBuilder.AddColumn<string>(
                name: "CountryCode",
                schema: "catalog",
                table: "Brand",
                type: "nchar(2)",
                fixedLength: true,
                maxLength: 2,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Brand_CountryCode",
                schema: "catalog",
                table: "Brand",
                column: "CountryCode");

            migrationBuilder.AddForeignKey(
                name: "FK_Brand_Country_CountryCode",
                schema: "catalog",
                table: "Brand",
                column: "CountryCode",
                principalSchema: "catalog",
                principalTable: "Country",
                principalColumn: "Code2",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
