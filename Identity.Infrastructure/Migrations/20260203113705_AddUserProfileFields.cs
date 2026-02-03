using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Identity.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUserProfileFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Address",
                schema: "identity",
                table: "User",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "BirthDateUtc",
                schema: "identity",
                table: "User",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Landline",
                schema: "identity",
                table: "User",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NationalId",
                schema: "identity",
                table: "User",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PostalCode",
                schema: "identity",
                table: "User",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Address",
                schema: "identity",
                table: "User");

            migrationBuilder.DropColumn(
                name: "BirthDateUtc",
                schema: "identity",
                table: "User");

            migrationBuilder.DropColumn(
                name: "Landline",
                schema: "identity",
                table: "User");

            migrationBuilder.DropColumn(
                name: "NationalId",
                schema: "identity",
                table: "User");

            migrationBuilder.DropColumn(
                name: "PostalCode",
                schema: "identity",
                table: "User");
        }
    }
}
