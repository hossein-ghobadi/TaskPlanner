using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TaskPlanner.Persistence.Migrations.MVPTestDatabase
{
    /// <inheritdoc />
    public partial class Edit_Task_planning : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TaskCategories_TaskCategories_ParentId",
                table: "TaskCategories");

            migrationBuilder.DropIndex(
                name: "IX_TaskCategories_ParentId",
                table: "TaskCategories");

            migrationBuilder.DropColumn(
                name: "ParentId",
                table: "TaskCategories");

            migrationBuilder.AddColumn<int>(
                name: "ParentId",
                table: "TaskItems",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_TaskItems_ParentId",
                table: "TaskItems",
                column: "ParentId");

            migrationBuilder.AddForeignKey(
                name: "FK_TaskItems_TaskItems_ParentId",
                table: "TaskItems",
                column: "ParentId",
                principalTable: "TaskItems",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TaskItems_TaskItems_ParentId",
                table: "TaskItems");

            migrationBuilder.DropIndex(
                name: "IX_TaskItems_ParentId",
                table: "TaskItems");

            migrationBuilder.DropColumn(
                name: "ParentId",
                table: "TaskItems");

            migrationBuilder.AddColumn<int>(
                name: "ParentId",
                table: "TaskCategories",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_TaskCategories_ParentId",
                table: "TaskCategories",
                column: "ParentId");

            migrationBuilder.AddForeignKey(
                name: "FK_TaskCategories_TaskCategories_ParentId",
                table: "TaskCategories",
                column: "ParentId",
                principalTable: "TaskCategories",
                principalColumn: "Id");
        }
    }
}
