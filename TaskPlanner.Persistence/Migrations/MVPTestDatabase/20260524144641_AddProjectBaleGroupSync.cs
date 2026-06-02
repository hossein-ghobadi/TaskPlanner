using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TaskPlanner.Persistence.Migrations.MVPTestDatabase
{
    /// <inheritdoc />
    public partial class AddProjectBaleGroupSync : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "ExternalMessageId",
                table: "ProjectChatMessages",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExternalProvider",
                table: "ProjectChatMessages",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "BaleBotSyncStates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LastUpdateId = table.Column<int>(type: "int", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BaleBotSyncStates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ProjectBaleGroupLinks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProjectId = table.Column<int>(type: "int", nullable: false),
                    ProjectChatGroupId = table.Column<int>(type: "int", nullable: false),
                    BaleChatId = table.Column<long>(type: "bigint", nullable: false),
                    BaleChatTitle = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    IsEnabled = table.Column<bool>(type: "bit", nullable: false),
                    CreatedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectBaleGroupLinks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProjectBaleGroupLinks_ProjectChatGroups_ProjectChatGroupId",
                        column: x => x.ProjectChatGroupId,
                        principalTable: "ProjectChatGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProjectBaleGroupLinks_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProjectChatMessages_ProjectChatGroupId_ExternalProvider_ExternalMessageId",
                table: "ProjectChatMessages",
                columns: new[] { "ProjectChatGroupId", "ExternalProvider", "ExternalMessageId" },
                unique: true,
                filter: "[ExternalMessageId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectBaleGroupLinks_BaleChatId",
                table: "ProjectBaleGroupLinks",
                column: "BaleChatId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectBaleGroupLinks_ProjectChatGroupId",
                table: "ProjectBaleGroupLinks",
                column: "ProjectChatGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectBaleGroupLinks_ProjectId_BaleChatId",
                table: "ProjectBaleGroupLinks",
                columns: new[] { "ProjectId", "BaleChatId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BaleBotSyncStates");

            migrationBuilder.DropTable(
                name: "ProjectBaleGroupLinks");

            migrationBuilder.DropIndex(
                name: "IX_ProjectChatMessages_ProjectChatGroupId_ExternalProvider_ExternalMessageId",
                table: "ProjectChatMessages");

            migrationBuilder.DropColumn(
                name: "ExternalMessageId",
                table: "ProjectChatMessages");

            migrationBuilder.DropColumn(
                name: "ExternalProvider",
                table: "ProjectChatMessages");
        }
    }
}
