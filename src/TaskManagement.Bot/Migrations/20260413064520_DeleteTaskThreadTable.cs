using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TaskManagement.Bot.Migrations
{
    /// <inheritdoc />
    public partial class DeleteTaskThreadTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TaskChannel_TaskItems_TaskItemId",
                table: "TaskChannel");

            migrationBuilder.DropTable(
                name: "TaskThreads");

            migrationBuilder.DropPrimaryKey(
                name: "PK_TaskChannel",
                table: "TaskChannel");

            migrationBuilder.RenameTable(
                name: "TaskChannel",
                newName: "TaskChannels");

            migrationBuilder.RenameIndex(
                name: "IX_TaskChannel_TaskItemId",
                table: "TaskChannels",
                newName: "IX_TaskChannels_TaskItemId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_TaskChannels",
                table: "TaskChannels",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_TaskChannels_TaskItems_TaskItemId",
                table: "TaskChannels",
                column: "TaskItemId",
                principalTable: "TaskItems",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TaskChannels_TaskItems_TaskItemId",
                table: "TaskChannels");

            migrationBuilder.DropPrimaryKey(
                name: "PK_TaskChannels",
                table: "TaskChannels");

            migrationBuilder.RenameTable(
                name: "TaskChannels",
                newName: "TaskChannel");

            migrationBuilder.RenameIndex(
                name: "IX_TaskChannels_TaskItemId",
                table: "TaskChannel",
                newName: "IX_TaskChannel_TaskItemId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_TaskChannel",
                table: "TaskChannel",
                column: "Id");

            migrationBuilder.CreateTable(
                name: "TaskThreads",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TaskItemId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    ThreadId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaskThreads", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TaskThreads_TaskItems_TaskItemId",
                        column: x => x.TaskItemId,
                        principalTable: "TaskItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TaskThreads_TaskItemId",
                table: "TaskThreads",
                column: "TaskItemId");

            migrationBuilder.AddForeignKey(
                name: "FK_TaskChannel_TaskItems_TaskItemId",
                table: "TaskChannel",
                column: "TaskItemId",
                principalTable: "TaskItems",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
