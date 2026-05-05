using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TaskPlanner.Persistence.Migrations.MVPTestDatabase
{
    /// <inheritdoc />
    public partial class change_TaskCategory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "TaskCategories",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "CreatorUserId",
                table: "TaskCategories",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "ProjectId",
                table: "TaskCategories",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_TaskCategories_ProjectId",
                table: "TaskCategories",
                column: "ProjectId");

            migrationBuilder.AddForeignKey(
                name: "FK_TaskCategories_Projects_ProjectId",
                table: "TaskCategories",
                column: "ProjectId",
                principalTable: "Projects",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TaskCategories_Projects_ProjectId",
                table: "TaskCategories");

            migrationBuilder.DropIndex(
                name: "IX_TaskCategories_ProjectId",
                table: "TaskCategories");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "TaskCategories");

            migrationBuilder.DropColumn(
                name: "CreatorUserId",
                table: "TaskCategories");

            migrationBuilder.DropColumn(
                name: "ProjectId",
                table: "TaskCategories");
        }
    }
}
