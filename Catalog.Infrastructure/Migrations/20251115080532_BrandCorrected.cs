using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Catalog.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class BrandCorrected : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MediaAsset",
                schema: "catalog",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StoredPath = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    ContentType = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    ThumbsJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MediaAsset", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Brand_LogoMediaId",
                schema: "catalog",
                table: "Brand",
                column: "LogoMediaId");

            migrationBuilder.Sql("UPDATE catalog.Brand SET LogoMediaId = NULL WHERE LogoMediaId IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_Brand_MediaAsset_LogoMediaId",
                schema: "catalog",
                table: "Brand",
                column: "LogoMediaId",
                principalSchema: "catalog",
                principalTable: "MediaAsset",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Brand_MediaAsset_LogoMediaId",
                schema: "catalog",
                table: "Brand");

            migrationBuilder.DropIndex(
                name: "IX_Brand_LogoMediaId",
                schema: "catalog",
                table: "Brand");

            migrationBuilder.DropTable(
                name: "MediaAsset",
                schema: "catalog");
        }
    }
}
