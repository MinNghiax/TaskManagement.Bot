using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using TaskManagement.Bot.Infrastructure.Data;

#nullable disable

namespace TaskManagement.Bot.Migrations
{
    /// <inheritdoc />
    [DbContextAttribute(typeof(TaskManagementDbContext))]
    [Migration("20260420093000_UpdateReminderRuleSchema")]
    public partial class UpdateReminderRuleSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ReminderRules_IsActive",
                table: "ReminderRules");

            migrationBuilder.AddColumn<bool>(
                name: "IsRepeat",
                table: "ReminderRules",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.Sql("""
                UPDATE [ReminderRules]
                SET [IsRepeat] = 1
                WHERE [TriggerType] = 2
                   OR [RepeatIntervalValue] IS NOT NULL
                   OR [RepeatIntervalUnit] IS NOT NULL;
                """);

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "ReminderRules");

            migrationBuilder.DropColumn(
                name: "Name",
                table: "ReminderRules");

            migrationBuilder.DropColumn(
                name: "RepeatIntervalUnit",
                table: "ReminderRules");

            migrationBuilder.DropColumn(
                name: "RepeatIntervalValue",
                table: "ReminderRules");

            migrationBuilder.Sql("""
                IF COL_LENGTH(N'dbo.ReminderRules', N'CreatedBy') IS NOT NULL
                BEGIN
                    ALTER TABLE [ReminderRules] DROP COLUMN [CreatedBy];
                END
                """);

            migrationBuilder.CreateIndex(
                name: "IX_ReminderRules_IsRepeat",
                table: "ReminderRules",
                column: "IsRepeat");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "ReminderRules",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "ReminderRules",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RepeatIntervalUnit",
                table: "ReminderRules",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "RepeatIntervalValue",
                table: "ReminderRules",
                type: "float",
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE [ReminderRules]
                SET [RepeatIntervalValue] = [Value],
                    [RepeatIntervalUnit] = [IntervalUnit]
                WHERE [IsRepeat] = 1;
                """);

            migrationBuilder.DropIndex(
                name: "IX_ReminderRules_IsRepeat",
                table: "ReminderRules");

            migrationBuilder.DropColumn(
                name: "IsRepeat",
                table: "ReminderRules");

            migrationBuilder.CreateIndex(
                name: "IX_ReminderRules_IsActive",
                table: "ReminderRules",
                column: "IsActive");
        }
    }
}
