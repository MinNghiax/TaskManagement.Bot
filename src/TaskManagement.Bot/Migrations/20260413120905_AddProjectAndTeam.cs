//using System;
//using Microsoft.EntityFrameworkCore.Migrations;

//#nullable disable

//namespace TaskManagement.Bot.Migrations
//{
//    /// <inheritdoc />
//    public partial class AddProjectAndTeam : Migration
//    {
//        /// <inheritdoc />
//        protected override void Up(MigrationBuilder migrationBuilder)
//        {
//            migrationBuilder.AddColumn<int>(
//                name: "TeamId",
//                table: "TaskItems",
//                type: "int",
//                nullable: true);

//            migrationBuilder.CreateTable(
//                name: "Projects",
//                columns: table => new
//                {
//                    Id = table.Column<int>(type: "int", nullable: false)
//                        .Annotation("SqlServer:Identity", "1, 1"),
//                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
//                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
//                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
//                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
//                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
//                },
//                constraints: table =>
//                {
//                    table.PrimaryKey("PK_Projects", x => x.Id);
//                });

//            migrationBuilder.CreateTable(
//                name: "Teams",
//                columns: table => new
//                {
//                    Id = table.Column<int>(type: "int", nullable: false)
//                        .Annotation("SqlServer:Identity", "1, 1"),
//                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
//                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
//                    ProjectId = table.Column<int>(type: "int", nullable: false),
//                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
//                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
//                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
//                },
//                constraints: table =>
//                {
//                    table.PrimaryKey("PK_Teams", x => x.Id);
//                    table.ForeignKey(
//                        name: "FK_Teams_Projects_ProjectId",
//                        column: x => x.ProjectId,
//                        principalTable: "Projects",
//                        principalColumn: "Id",
//                        onDelete: ReferentialAction.Cascade);
//                });

//            migrationBuilder.CreateTable(
//                name: "TeamMembers",
//                columns: table => new
//                {
//                    Id = table.Column<int>(type: "int", nullable: false)
//                        .Annotation("SqlServer:Identity", "1, 1"),
//                    Username = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
//                    Role = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
//                    TeamId = table.Column<int>(type: "int", nullable: false),
//                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
//                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
//                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
//                },
//                constraints: table =>
//                {
//                    table.PrimaryKey("PK_TeamMembers", x => x.Id);
//                    table.ForeignKey(
//                        name: "FK_TeamMembers_Teams_TeamId",
//                        column: x => x.TeamId,
//                        principalTable: "Teams",
//                        principalColumn: "Id",
//                        onDelete: ReferentialAction.Cascade);
//                });

//            migrationBuilder.CreateIndex(
//                name: "IX_TaskItems_TeamId",
//                table: "TaskItems",
//                column: "TeamId");

//            migrationBuilder.CreateIndex(
//                name: "IX_TeamMembers_TeamId",
//                table: "TeamMembers",
//                column: "TeamId");

//            migrationBuilder.CreateIndex(
//                name: "IX_TeamMembers_Username_TeamId",
//                table: "TeamMembers",
//                columns: new[] { "Username", "TeamId" },
//                unique: true);

//            migrationBuilder.CreateIndex(
//                name: "IX_Teams_ProjectId",
//                table: "Teams",
//                column: "ProjectId");

//            migrationBuilder.AddForeignKey(
//                name: "FK_TaskItems_Teams_TeamId",
//                table: "TaskItems",
//                column: "TeamId",
//                principalTable: "Teams",
//                principalColumn: "Id",
//                onDelete: ReferentialAction.SetNull);
//        }

//        /// <inheritdoc />
//        protected override void Down(MigrationBuilder migrationBuilder)
//        {
//            migrationBuilder.DropForeignKey(
//                name: "FK_TaskItems_Teams_TeamId",
//                table: "TaskItems");

//            migrationBuilder.DropTable(
//                name: "TeamMembers");

//            migrationBuilder.DropTable(
//                name: "Teams");

//            migrationBuilder.DropTable(
//                name: "Projects");

//            migrationBuilder.DropIndex(
//                name: "IX_TaskItems_TeamId",
//                table: "TaskItems");

//            migrationBuilder.DropColumn(
//                name: "TeamId",
//                table: "TaskItems");
//        }
//    }
//}
using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TaskManagement.Bot.Migrations
{
    public partial class AddProjectAndTeam : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 🔥 ADD TeamId nếu chưa có
            migrationBuilder.Sql(@"
            IF COL_LENGTH('TaskItems', 'TeamId') IS NULL
            BEGIN
                ALTER TABLE [TaskItems] ADD [TeamId] int NULL;
            END
            ");

            // 🔥 CREATE Projects nếu chưa có
            migrationBuilder.Sql(@"
            IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Projects' AND xtype='U')
            BEGIN
                CREATE TABLE [Projects] (
                    [Id] int IDENTITY(1,1) PRIMARY KEY,
                    [Name] nvarchar(200) NOT NULL,
                    [CreatedBy] nvarchar(100) NOT NULL,
                    [CreatedAt] datetime2 NOT NULL DEFAULT GETUTCDATE(),
                    [UpdatedAt] datetime2 NULL,
                    [IsDeleted] bit NOT NULL
                );
            END
            ");

            // 🔥 CREATE Teams nếu chưa có
            migrationBuilder.Sql(@"
            IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Teams' AND xtype='U')
            BEGIN
                CREATE TABLE [Teams] (
                    [Id] int IDENTITY(1,1) PRIMARY KEY,
                    [Name] nvarchar(200) NOT NULL,
                    [CreatedBy] nvarchar(100) NOT NULL,
                    [ProjectId] int NOT NULL,
                    [CreatedAt] datetime2 NOT NULL,
                    [UpdatedAt] datetime2 NULL,
                    [IsDeleted] bit NOT NULL,
                    CONSTRAINT [FK_Teams_Projects_ProjectId]
                        FOREIGN KEY ([ProjectId]) REFERENCES [Projects]([Id]) ON DELETE CASCADE
                );

                CREATE INDEX [IX_Teams_ProjectId] ON [Teams]([ProjectId]);
            END
            ");

            // 🔥 CREATE TeamMembers nếu chưa có
            migrationBuilder.Sql(@"
            IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='TeamMembers' AND xtype='U')
            BEGIN
                CREATE TABLE [TeamMembers] (
                    [Id] int IDENTITY(1,1) PRIMARY KEY,
                    [Username] nvarchar(100) NOT NULL,
                    [Role] nvarchar(20) NOT NULL,
                    [TeamId] int NOT NULL,
                    [CreatedAt] datetime2 NOT NULL,
                    [UpdatedAt] datetime2 NULL,
                    [IsDeleted] bit NOT NULL,
                    CONSTRAINT [FK_TeamMembers_Teams_TeamId]
                        FOREIGN KEY ([TeamId]) REFERENCES [Teams]([Id]) ON DELETE CASCADE
                );

                CREATE INDEX [IX_TeamMembers_TeamId] ON [TeamMembers]([TeamId]);
                CREATE UNIQUE INDEX [IX_TeamMembers_Username_TeamId]
                    ON [TeamMembers]([Username], [TeamId]);
            END
            ");

            // 🔥 INDEX cho TaskItems.TeamId
            migrationBuilder.Sql(@"
            IF NOT EXISTS (
                SELECT * FROM sys.indexes WHERE name = 'IX_TaskItems_TeamId'
            )
            BEGIN
                CREATE INDEX [IX_TaskItems_TeamId] ON [TaskItems]([TeamId]);
            END
            ");

            // 🔥 FK TaskItems → Teams
            migrationBuilder.Sql(@"
            IF NOT EXISTS (
                SELECT * FROM sys.foreign_keys WHERE name = 'FK_TaskItems_Teams_TeamId'
            )
            BEGIN
                ALTER TABLE [TaskItems]
                ADD CONSTRAINT [FK_TaskItems_Teams_TeamId]
                FOREIGN KEY ([TeamId]) REFERENCES [Teams]([Id])
                ON DELETE SET NULL;
            END
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // ⚠️ chỉ drop nếu tồn tại
            migrationBuilder.Sql(@"
            IF EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_TaskItems_Teams_TeamId')
            BEGIN
                ALTER TABLE [TaskItems] DROP CONSTRAINT [FK_TaskItems_Teams_TeamId];
            END
            ");

            migrationBuilder.Sql(@"
            IF EXISTS (SELECT * FROM sysobjects WHERE name='TeamMembers' AND xtype='U')
                DROP TABLE [TeamMembers];
            ");

            migrationBuilder.Sql(@"
            IF EXISTS (SELECT * FROM sysobjects WHERE name='Teams' AND xtype='U')
                DROP TABLE [Teams];
            ");

            migrationBuilder.Sql(@"
            IF EXISTS (SELECT * FROM sysobjects WHERE name='Projects' AND xtype='U')
                DROP TABLE [Projects];
            ");

            migrationBuilder.Sql(@"
            IF EXISTS (
                SELECT * FROM sys.indexes WHERE name = 'IX_TaskItems_TeamId'
            )
            DROP INDEX [IX_TaskItems_TeamId] ON [TaskItems];
            ");

            migrationBuilder.Sql(@"
            IF COL_LENGTH('TaskItems', 'TeamId') IS NOT NULL
            BEGIN
                ALTER TABLE [TaskItems] DROP COLUMN [TeamId];
            END
            ");
        }
    }
}
