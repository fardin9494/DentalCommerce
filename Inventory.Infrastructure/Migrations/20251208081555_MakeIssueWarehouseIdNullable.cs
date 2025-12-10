using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Inventory.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class MakeIssueWarehouseIdNullable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<Guid>(
                name: "WarehouseId",
                table: "Issue",
                schema: "inv",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // قبل از غیر nullable کردن، باید رکوردهای null را به یک انبار پیش‌فرض اختصاص دهیم
            // یا آنها را حذف کنیم. برای امنیت، این migration را rollback نمی‌کنیم.
            // اگر نیاز به rollback دارید، باید به صورت دستی رکوردهای null را مدیریت کنید.
            
            // migrationBuilder.AlterColumn<Guid>(
            //     name: "WarehouseId",
            //     table: "Issue",
            //     schema: "inv",
            //     type: "uniqueidentifier",
            //     nullable: false,
            //     defaultValue: Guid.Empty,
            //     oldClrType: typeof(Guid?),
            //     oldType: "uniqueidentifier",
            //     oldNullable: true);
        }
    }
}
