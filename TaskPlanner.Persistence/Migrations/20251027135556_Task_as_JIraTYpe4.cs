using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TaskPlanner.Persistence.Migrations.MVPTestDatabase
{
    /// <inheritdoc />
    public partial class Task_as_JIraTYpe4 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TaskItems_User_AssignedUserId",
                table: "TaskItems");

            migrationBuilder.AddForeignKey(
                name: "FK_TaskItems_User_AssignedUserId",
                table: "TaskItems",
                column: "AssignedUserId",
                principalTable: "User",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TaskItems_User_AssignedUserId",
                table: "TaskItems");

            migrationBuilder.AddForeignKey(
                name: "FK_TaskItems_User_AssignedUserId",
                table: "TaskItems",
                column: "AssignedUserId",
                principalTable: "User",
                principalColumn: "Id");
        }
    }
}
