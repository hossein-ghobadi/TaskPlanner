using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TaskPlanner.Persistence.Migrations.MVPTestDatabase
{
    /// <inheritdoc />
    public partial class leadnotecorrect : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "ProjectId",
                table: "ProjectNotes",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<int>(
                name: "LeadId",
                table: "ProjectNotes",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProjectNotes_LeadId",
                table: "ProjectNotes",
                column: "LeadId");

            migrationBuilder.AddForeignKey(
                name: "FK_ProjectNotes_Leads_LeadId",
                table: "ProjectNotes",
                column: "LeadId",
                principalTable: "Leads",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProjectNotes_Leads_LeadId",
                table: "ProjectNotes");

            migrationBuilder.DropIndex(
                name: "IX_ProjectNotes_LeadId",
                table: "ProjectNotes");

            migrationBuilder.DropColumn(
                name: "LeadId",
                table: "ProjectNotes");

            migrationBuilder.AlterColumn<int>(
                name: "ProjectId",
                table: "ProjectNotes",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);
        }
    }
}
