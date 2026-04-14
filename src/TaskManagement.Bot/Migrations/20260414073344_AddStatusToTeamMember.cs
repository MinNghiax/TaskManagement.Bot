using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TaskManagement.Bot.Migrations
{
    /// <inheritdoc />
    public partial class AddStatusToTeamMember : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "TeamMembers",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Status",
                table: "TeamMembers");
        }
    }
}
