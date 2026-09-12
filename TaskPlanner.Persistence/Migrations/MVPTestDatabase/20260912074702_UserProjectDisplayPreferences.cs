using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TaskPlanner.Persistence.Migrations.MVPTestDatabase
{
    /// <inheritdoc />
    public partial class UserProjectDisplayPreferences : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UserProjectDisplayPreferences",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    ProjectId = table.Column<int>(type: "int", nullable: false),
                    ShowTasks = table.Column<bool>(type: "bit", nullable: false),
                    ShowKanban = table.Column<bool>(type: "bit", nullable: false),
                    ShowSprints = table.Column<bool>(type: "bit", nullable: false),
                    ShowFeatures = table.Column<bool>(type: "bit", nullable: false),
                    ShowTickets = table.Column<bool>(type: "bit", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserProjectDisplayPreferences", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserProjectDisplayPreferences_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserProjectDisplayPreferences_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserProjectDisplayPreferences_ProjectId",
                table: "UserProjectDisplayPreferences",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_UserProjectDisplayPreferences_UserId_ProjectId",
                table: "UserProjectDisplayPreferences",
                columns: new[] { "UserId", "ProjectId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserProjectDisplayPreferences");
        }
    }
}
