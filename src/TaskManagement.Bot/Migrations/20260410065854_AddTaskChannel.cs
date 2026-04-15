//using System;
//using Microsoft.EntityFrameworkCore.Migrations;

//#nullable disable

//namespace TaskManagement.Bot.Migrations
//{
//    /// <inheritdoc />
//    public partial class AddTaskChannel : Migration
//    {
//        /// <inheritdoc />
//        protected override void Up(MigrationBuilder migrationBuilder)
//        {
//            migrationBuilder.CreateTable(
//                name: "TaskChannel",
//                columns: table => new
//                {
//                    Id = table.Column<int>(type: "int", nullable: false)
//                        .Annotation("SqlServer:Identity", "1, 1"),
//                    ChannelId = table.Column<string>(type: "nvarchar(max)", nullable: false),
//                    TaskItemId = table.Column<int>(type: "int", nullable: false),
//                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
//                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
//                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
//                },
//                constraints: table =>
//                {
//                    table.PrimaryKey("PK_TaskChannel", x => x.Id);
//                    table.ForeignKey(
//                        name: "FK_TaskChannel_TaskItems_TaskItemId",
//                        column: x => x.TaskItemId,
//                        principalTable: "TaskItems",
//                        principalColumn: "Id",
//                        onDelete: ReferentialAction.Cascade);
//                });

//            migrationBuilder.CreateIndex(
//                name: "IX_TaskChannel_TaskItemId",
//                table: "TaskChannel",
//                column: "TaskItemId");
//        }

//        /// <inheritdoc />
//        protected override void Down(MigrationBuilder migrationBuilder)
//        {
//            migrationBuilder.DropTable(
//                name: "TaskChannel");
//        }
//    }
//}
using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TaskManagement.Bot.Migrations
{
    public partial class AddTaskChannel : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 🔥 CHỈ TẠO TABLE NẾU CHƯA TỒN TẠI
            migrationBuilder.Sql(@"
            IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='TaskChannel' AND xtype='U')
            BEGIN
                CREATE TABLE [TaskChannel] (
                    [Id] int NOT NULL IDENTITY(1,1),
                    [ChannelId] nvarchar(max) NOT NULL,
                    [TaskItemId] int NOT NULL,
                    [CreatedAt] datetime2 NOT NULL,
                    [UpdatedAt] datetime2 NULL,
                    [IsDeleted] bit NOT NULL,
                    CONSTRAINT [PK_TaskChannel] PRIMARY KEY ([Id]),
                    CONSTRAINT [FK_TaskChannel_TaskItems_TaskItemId]
                        FOREIGN KEY ([TaskItemId]) REFERENCES [TaskItems] ([Id]) ON DELETE CASCADE
                );

                CREATE INDEX [IX_TaskChannel_TaskItemId]
                ON [TaskChannel] ([TaskItemId]);
            END
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // ⚠️ chỉ drop nếu tồn tại
            migrationBuilder.Sql(@"
            IF EXISTS (SELECT * FROM sysobjects WHERE name='TaskChannel' AND xtype='U')
            BEGIN
                DROP TABLE [TaskChannel];
            END
            ");
        }
    }
}