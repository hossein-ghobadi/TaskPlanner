using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TaskPlanner.Persistence.Migrations.MVPTestDatabase
{
    /// <inheritdoc />
    public partial class addticketToTaskItem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "TicketId",
                table: "TaskItems",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_TaskItems_TicketId",
                table: "TaskItems",
                column: "TicketId");

            migrationBuilder.AddForeignKey(
                name: "FK_TaskItems_ProjectTickets_TicketId",
                table: "TaskItems",
                column: "TicketId",
                principalTable: "ProjectTickets",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TaskItems_ProjectTickets_TicketId",
                table: "TaskItems");

            migrationBuilder.DropIndex(
                name: "IX_TaskItems_TicketId",
                table: "TaskItems");

            migrationBuilder.DropColumn(
                name: "TicketId",
                table: "TaskItems");
        }
    }
}
