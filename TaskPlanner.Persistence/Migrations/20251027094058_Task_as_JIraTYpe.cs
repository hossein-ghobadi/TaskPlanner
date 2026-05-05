using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TaskPlanner.Persistence.Migrations.MVPTestDatabase
{
    /// <inheritdoc />
    public partial class Task_as_JIraTYpe : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TaskItems_TaskCategories_CategoryId",
                table: "TaskItems");

            migrationBuilder.DropForeignKey(
                name: "FK_TaskItems_TaskItems_ParentId",
                table: "TaskItems");

            migrationBuilder.DropIndex(
                name: "IX_TaskItems_ProjectId",
                table: "TaskItems");

            migrationBuilder.RenameColumn(
                name: "ParentId",
                table: "TaskItems",
                newName: "TaskCategoryId");

            migrationBuilder.RenameIndex(
                name: "IX_TaskItems_ParentId",
                table: "TaskItems",
                newName: "IX_TaskItems_TaskCategoryId");

            migrationBuilder.AlterColumn<string>(
                name: "Title",
                table: "TaskItems",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<int>(
                name: "CategoryId",
                table: "TaskItems",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "TaskItems",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "CreatedByUserId",
                table: "TaskItems",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IssueKey",
                table: "TaskItems",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "IssueType",
                table: "TaskItems",
                type: "int",
                nullable: false,
                defaultValue: 3);

            migrationBuilder.AddColumn<decimal>(
                name: "OriginalEstimateHours",
                table: "TaskItems",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ParentTaskId",
                table: "TaskItems",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "RemainingTimeHours",
                table: "TaskItems",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "StoryPoints",
                table: "TaskItems",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TimeSpentHours",
                table: "TaskItems",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "TaskItems",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Projects",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "Projects",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "IssueKeyPrefix",
                table: "Projects",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "LastIssueNumber",
                table: "Projects",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "Projects",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.CreateIndex(
                name: "IX_TaskItems_IssueKey",
                table: "TaskItems",
                column: "IssueKey",
                unique: true,
                filter: "[IssueKey] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_TaskItems_ParentTaskId",
                table: "TaskItems",
                column: "ParentTaskId");

            migrationBuilder.CreateIndex(
                name: "IX_TaskItems_ProjectId_IssueType",
                table: "TaskItems",
                columns: new[] { "ProjectId", "IssueType" });

            migrationBuilder.CreateIndex(
                name: "IX_Projects_IssueKeyPrefix",
                table: "Projects",
                column: "IssueKeyPrefix");

            migrationBuilder.AddForeignKey(
                name: "FK_TaskItems_TaskCategories_CategoryId",
                table: "TaskItems",
                column: "CategoryId",
                principalTable: "TaskCategories",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_TaskItems_TaskCategories_TaskCategoryId",
                table: "TaskItems",
                column: "TaskCategoryId",
                principalTable: "TaskCategories",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_TaskItems_TaskItems_ParentTaskId",
                table: "TaskItems",
                column: "ParentTaskId",
                principalTable: "TaskItems",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TaskItems_TaskCategories_CategoryId",
                table: "TaskItems");

            migrationBuilder.DropForeignKey(
                name: "FK_TaskItems_TaskCategories_TaskCategoryId",
                table: "TaskItems");

            migrationBuilder.DropForeignKey(
                name: "FK_TaskItems_TaskItems_ParentTaskId",
                table: "TaskItems");

            migrationBuilder.DropIndex(
                name: "IX_TaskItems_IssueKey",
                table: "TaskItems");

            migrationBuilder.DropIndex(
                name: "IX_TaskItems_ParentTaskId",
                table: "TaskItems");

            migrationBuilder.DropIndex(
                name: "IX_TaskItems_ProjectId_IssueType",
                table: "TaskItems");

            migrationBuilder.DropIndex(
                name: "IX_Projects_IssueKeyPrefix",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "TaskItems");

            migrationBuilder.DropColumn(
                name: "CreatedByUserId",
                table: "TaskItems");

            migrationBuilder.DropColumn(
                name: "IssueKey",
                table: "TaskItems");

            migrationBuilder.DropColumn(
                name: "IssueType",
                table: "TaskItems");

            migrationBuilder.DropColumn(
                name: "OriginalEstimateHours",
                table: "TaskItems");

            migrationBuilder.DropColumn(
                name: "ParentTaskId",
                table: "TaskItems");

            migrationBuilder.DropColumn(
                name: "RemainingTimeHours",
                table: "TaskItems");

            migrationBuilder.DropColumn(
                name: "StoryPoints",
                table: "TaskItems");

            migrationBuilder.DropColumn(
                name: "TimeSpentHours",
                table: "TaskItems");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "TaskItems");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "IssueKeyPrefix",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "LastIssueNumber",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "Projects");

            migrationBuilder.RenameColumn(
                name: "TaskCategoryId",
                table: "TaskItems",
                newName: "ParentId");

            migrationBuilder.RenameIndex(
                name: "IX_TaskItems_TaskCategoryId",
                table: "TaskItems",
                newName: "IX_TaskItems_ParentId");

            migrationBuilder.AlterColumn<string>(
                name: "Title",
                table: "TaskItems",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500);

            migrationBuilder.AlterColumn<int>(
                name: "CategoryId",
                table: "TaskItems",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Projects",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200);

            migrationBuilder.CreateIndex(
                name: "IX_TaskItems_ProjectId",
                table: "TaskItems",
                column: "ProjectId");

            migrationBuilder.AddForeignKey(
                name: "FK_TaskItems_TaskCategories_CategoryId",
                table: "TaskItems",
                column: "CategoryId",
                principalTable: "TaskCategories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TaskItems_TaskItems_ParentId",
                table: "TaskItems",
                column: "ParentId",
                principalTable: "TaskItems",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
