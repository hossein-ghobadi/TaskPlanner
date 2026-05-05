using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TaskPlanner.Persistence.Migrations.MVPTestDatabase
{
    /// <inheritdoc />
    public partial class workflow_correction : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SprintId",
                table: "WorkflowStatuses",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowStatuses_SprintId",
                table: "WorkflowStatuses",
                column: "SprintId");

            migrationBuilder.AddForeignKey(
                name: "FK_WorkflowStatuses_Sprints_SprintId",
                table: "WorkflowStatuses",
                column: "SprintId",
                principalTable: "Sprints",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_WorkflowStatuses_Sprints_SprintId",
                table: "WorkflowStatuses");

            migrationBuilder.DropIndex(
                name: "IX_WorkflowStatuses_SprintId",
                table: "WorkflowStatuses");

            migrationBuilder.DropColumn(
                name: "SprintId",
                table: "WorkflowStatuses");
        }
    }
}
