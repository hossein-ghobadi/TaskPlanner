using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TaskPlanner.Persistence.Migrations.MVPTestDatabase
{
    /// <inheritdoc />
    public partial class AddProjectIssueTypes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ProjectIssueTypeId",
                table: "TaskItems",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ProjectIssueTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProjectId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Icon = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    Color = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    Order = table.Column<int>(type: "int", nullable: false),
                    BaseType = table.Column<int>(type: "int", nullable: false),
                    IsCustom = table.Column<bool>(type: "bit", nullable: false),
                    CanAddToSprint = table.Column<bool>(type: "bit", nullable: false),
                    CanHaveChildren = table.Column<bool>(type: "bit", nullable: false),
                    IncludeInReports = table.Column<bool>(type: "bit", nullable: false),
                    CreatedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectIssueTypes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProjectIssueTypes_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TaskItems_ProjectIssueTypeId",
                table: "TaskItems",
                column: "ProjectIssueTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectIssueTypes_ProjectId_Name",
                table: "ProjectIssueTypes",
                columns: new[] { "ProjectId", "Name" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_TaskItems_ProjectIssueTypes_ProjectIssueTypeId",
                table: "TaskItems",
                column: "ProjectIssueTypeId",
                principalTable: "ProjectIssueTypes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TaskItems_ProjectIssueTypes_ProjectIssueTypeId",
                table: "TaskItems");

            migrationBuilder.DropTable(
                name: "ProjectIssueTypes");

            migrationBuilder.DropIndex(
                name: "IX_TaskItems_ProjectIssueTypeId",
                table: "TaskItems");

            migrationBuilder.DropColumn(
                name: "ProjectIssueTypeId",
                table: "TaskItems");
        }
    }
}
