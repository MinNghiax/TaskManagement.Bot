using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TaskManagement.Bot.Migrations
{
    /// <inheritdoc />
    public partial class EditTableName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Complains_Tasks_TaskItemId",
                table: "Complains");

            migrationBuilder.DropForeignKey(
                name: "FK_Reminders_Tasks_TaskId",
                table: "Reminders");

            migrationBuilder.DropForeignKey(
                name: "FK_TaskClans_Tasks_TaskItemId",
                table: "TaskClans");

            migrationBuilder.DropForeignKey(
                name: "FK_TaskThreads_Tasks_TaskItemId",
                table: "TaskThreads");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Tasks",
                table: "Tasks");

            migrationBuilder.RenameTable(
                name: "Tasks",
                newName: "TaskItems");

            migrationBuilder.RenameIndex(
                name: "IX_Tasks_Status",
                table: "TaskItems",
                newName: "IX_TaskItems_Status");

            migrationBuilder.RenameIndex(
                name: "IX_Tasks_CreatedAt",
                table: "TaskItems",
                newName: "IX_TaskItems_CreatedAt");

            migrationBuilder.RenameIndex(
                name: "IX_Tasks_AssignedTo",
                table: "TaskItems",
                newName: "IX_TaskItems_AssignedTo");

            migrationBuilder.AddPrimaryKey(
                name: "PK_TaskItems",
                table: "TaskItems",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Complains_TaskItems_TaskItemId",
                table: "Complains",
                column: "TaskItemId",
                principalTable: "TaskItems",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Reminders_TaskItems_TaskId",
                table: "Reminders",
                column: "TaskId",
                principalTable: "TaskItems",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TaskClans_TaskItems_TaskItemId",
                table: "TaskClans",
                column: "TaskItemId",
                principalTable: "TaskItems",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TaskThreads_TaskItems_TaskItemId",
                table: "TaskThreads",
                column: "TaskItemId",
                principalTable: "TaskItems",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Complains_TaskItems_TaskItemId",
                table: "Complains");

            migrationBuilder.DropForeignKey(
                name: "FK_Reminders_TaskItems_TaskId",
                table: "Reminders");

            migrationBuilder.DropForeignKey(
                name: "FK_TaskClans_TaskItems_TaskItemId",
                table: "TaskClans");

            migrationBuilder.DropForeignKey(
                name: "FK_TaskThreads_TaskItems_TaskItemId",
                table: "TaskThreads");

            migrationBuilder.DropPrimaryKey(
                name: "PK_TaskItems",
                table: "TaskItems");

            migrationBuilder.RenameTable(
                name: "TaskItems",
                newName: "Tasks");

            migrationBuilder.RenameIndex(
                name: "IX_TaskItems_Status",
                table: "Tasks",
                newName: "IX_Tasks_Status");

            migrationBuilder.RenameIndex(
                name: "IX_TaskItems_CreatedAt",
                table: "Tasks",
                newName: "IX_Tasks_CreatedAt");

            migrationBuilder.RenameIndex(
                name: "IX_TaskItems_AssignedTo",
                table: "Tasks",
                newName: "IX_Tasks_AssignedTo");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Tasks",
                table: "Tasks",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Complains_Tasks_TaskItemId",
                table: "Complains",
                column: "TaskItemId",
                principalTable: "Tasks",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Reminders_Tasks_TaskId",
                table: "Reminders",
                column: "TaskId",
                principalTable: "Tasks",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TaskClans_Tasks_TaskItemId",
                table: "TaskClans",
                column: "TaskItemId",
                principalTable: "Tasks",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TaskThreads_Tasks_TaskItemId",
                table: "TaskThreads",
                column: "TaskItemId",
                principalTable: "Tasks",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
