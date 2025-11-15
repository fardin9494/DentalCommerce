using Microsoft.EntityFrameworkCore.Migrations;

// NOTE: Timestamp in classname/filename is illustrative; adjust to your migration timestamping.
namespace Catalog.Infrastructure.Migrations
{
    public partial class RemoveBrandCountry : Migration
    {
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
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
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

