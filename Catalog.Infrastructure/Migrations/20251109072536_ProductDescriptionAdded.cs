using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Catalog.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ProductDescriptionAdded : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Store_Domain",
                schema: "catalog",
                table: "Store");

            migrationBuilder.AddColumn<string>(
                name: "Description",
                schema: "catalog",
                table: "Product",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Store_Domain",
                schema: "catalog",
                table: "Store",
                column: "Domain",
                unique: true,
                filter: "[Domain] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Store_Name",
                schema: "catalog",
                table: "Store",
                column: "Name");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Store_Domain",
                schema: "catalog",
                table: "Store");

            migrationBuilder.DropIndex(
                name: "IX_Store_Name",
                schema: "catalog",
                table: "Store");

            migrationBuilder.DropColumn(
                name: "Description",
                schema: "catalog",
                table: "Product");

            migrationBuilder.CreateIndex(
                name: "IX_Store_Domain",
                schema: "catalog",
                table: "Store",
                column: "Domain");
        }
    }
}
