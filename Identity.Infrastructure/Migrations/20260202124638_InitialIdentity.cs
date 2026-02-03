using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Identity.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialIdentity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "identity");

            migrationBuilder.CreateTable(
                name: "OtpChallenge",
                schema: "identity",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PhoneNumber = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    SiteId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Purpose = table.Column<int>(type: "int", nullable: false),
                    CodeHash = table.Column<byte[]>(type: "varbinary(32)", nullable: false),
                    Salt = table.Column<byte[]>(type: "varbinary(32)", nullable: false),
                    ExpiresAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    VerifiedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AttemptCount = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OtpChallenge", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "User",
                schema: "identity",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PhoneNumber = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    PhoneVerifiedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FullName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    PasswordHash = table.Column<byte[]>(type: "varbinary(64)", nullable: true),
                    PasswordSalt = table.Column<byte[]>(type: "varbinary(32)", nullable: true),
                    PasswordIterations = table.Column<int>(type: "int", nullable: true),
                    PasswordAlgorithm = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    PasswordSetAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_User", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UserSession",
                schema: "identity",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SiteId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RefreshTokenHash = table.Column<byte[]>(type: "varbinary(32)", nullable: false),
                    ExpiresAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RevokedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReplacedBySessionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UserAgent = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    IpAddress = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserSession", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserSession_User_UserId",
                        column: x => x.UserId,
                        principalSchema: "identity",
                        principalTable: "User",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserSiteMembership",
                schema: "identity",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SiteId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserSiteMembership", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserSiteMembership_User_UserId",
                        column: x => x.UserId,
                        principalSchema: "identity",
                        principalTable: "User",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OtpChallenge_ExpiresAtUtc",
                schema: "identity",
                table: "OtpChallenge",
                column: "ExpiresAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_OtpChallenge_PhoneNumber_SiteId_CreatedAt",
                schema: "identity",
                table: "OtpChallenge",
                columns: new[] { "PhoneNumber", "SiteId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_User_PhoneNumber",
                schema: "identity",
                table: "User",
                column: "PhoneNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserSession_ExpiresAtUtc",
                schema: "identity",
                table: "UserSession",
                column: "ExpiresAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_UserSession_RefreshTokenHash",
                schema: "identity",
                table: "UserSession",
                column: "RefreshTokenHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserSession_UserId_SiteId",
                schema: "identity",
                table: "UserSession",
                columns: new[] { "UserId", "SiteId" });

            migrationBuilder.CreateIndex(
                name: "IX_UserSiteMembership_UserId_SiteId",
                schema: "identity",
                table: "UserSiteMembership",
                columns: new[] { "UserId", "SiteId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OtpChallenge",
                schema: "identity");

            migrationBuilder.DropTable(
                name: "UserSession",
                schema: "identity");

            migrationBuilder.DropTable(
                name: "UserSiteMembership",
                schema: "identity");

            migrationBuilder.DropTable(
                name: "User",
                schema: "identity");
        }
    }
}
