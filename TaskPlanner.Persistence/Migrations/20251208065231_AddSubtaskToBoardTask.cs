using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TaskPlanner.Persistence.Migrations.MVPTestDatabase
{
    /// <inheritdoc />
    public partial class AddSubtaskToBoardTask : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ParentTaskId",
                table: "BoardTasks",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_BoardTasks_ParentTaskId",
                table: "BoardTasks",
                column: "ParentTaskId");

            migrationBuilder.AddForeignKey(
                name: "FK_BoardTasks_BoardTasks_ParentTaskId",
                table: "BoardTasks",
                column: "ParentTaskId",
                principalTable: "BoardTasks",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BoardTasks_BoardTasks_ParentTaskId",
                table: "BoardTasks");

            migrationBuilder.DropIndex(
                name: "IX_BoardTasks_ParentTaskId",
                table: "BoardTasks");

            migrationBuilder.DropColumn(
                name: "ParentTaskId",
                table: "BoardTasks");
        }
    }
}
