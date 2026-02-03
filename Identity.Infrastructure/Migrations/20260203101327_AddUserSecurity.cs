using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Identity.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUserSecurity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BanReason",
                schema: "identity",
                table: "User",
                type: "nvarchar(512)",
                maxLength: 512,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "BanUntilUtc",
                schema: "identity",
                table: "User",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "BannedAtUtc",
                schema: "identity",
                table: "User",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsBanned",
                schema: "identity",
                table: "User",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastLoginAtUtc",
                schema: "identity",
                table: "User",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "LoginGuard",
                schema: "identity",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PhoneNumber = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    FailedCount = table.Column<int>(type: "int", nullable: false),
                    LastFailedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LockedUntilUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LoginGuard", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LoginGuard_PhoneNumber",
                schema: "identity",
                table: "LoginGuard",
                column: "PhoneNumber",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LoginGuard",
                schema: "identity");

            migrationBuilder.DropColumn(
                name: "BanReason",
                schema: "identity",
                table: "User");

            migrationBuilder.DropColumn(
                name: "BanUntilUtc",
                schema: "identity",
                table: "User");

            migrationBuilder.DropColumn(
                name: "BannedAtUtc",
                schema: "identity",
                table: "User");

            migrationBuilder.DropColumn(
                name: "IsBanned",
                schema: "identity",
                table: "User");

            migrationBuilder.DropColumn(
                name: "LastLoginAtUtc",
                schema: "identity",
                table: "User");
        }
    }
}
