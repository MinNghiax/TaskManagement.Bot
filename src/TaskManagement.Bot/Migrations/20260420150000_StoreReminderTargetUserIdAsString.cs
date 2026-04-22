using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TaskManagement.Bot.Migrations
{
    /// <inheritdoc />
    public partial class StoreReminderTargetUserIdAsString : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "TargetUserId",
                table: "Reminders",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE [Reminders]
                SET [TargetUserId] = N'0'
                WHERE TRY_CONVERT(int, [TargetUserId]) IS NULL;
                """);

            migrationBuilder.AlterColumn<int>(
                name: "TargetUserId",
                table: "Reminders",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);
        }
    }
}
