using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using TaskManagement.Bot.Infrastructure.Data;

#nullable disable

namespace TaskManagement.Bot.Migrations
{
    [Migration("20260421070000_AddTaskReviewStartedAt")]
    [DbContext(typeof(TaskManagementDbContext))]
    public partial class AddTaskReviewStartedAt : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ReviewStartedAt",
                table: "TaskItems",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_TaskItems_ReviewStartedAt",
                table: "TaskItems",
                column: "ReviewStartedAt");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TaskItems_ReviewStartedAt",
                table: "TaskItems");

            migrationBuilder.DropColumn(
                name: "ReviewStartedAt",
                table: "TaskItems");
        }
    }
}
