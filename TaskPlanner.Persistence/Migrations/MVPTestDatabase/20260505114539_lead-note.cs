using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TaskPlanner.Persistence.Migrations.MVPTestDatabase
{
    /// <inheritdoc />
    public partial class leadnote : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SourceLeadId",
                table: "ProjectNotes",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "LeadNotes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LeadId = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Content = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LeadNotes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LeadNotes_Leads_LeadId",
                        column: x => x.LeadId,
                        principalTable: "Leads",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProjectNotes_SourceLeadId",
                table: "ProjectNotes",
                column: "SourceLeadId");

            migrationBuilder.CreateIndex(
                name: "IX_LeadNotes_LeadId_CreatedAt",
                table: "LeadNotes",
                columns: new[] { "LeadId", "CreatedAt" });

            migrationBuilder.AddForeignKey(
                name: "FK_ProjectNotes_Leads_SourceLeadId",
                table: "ProjectNotes",
                column: "SourceLeadId",
                principalTable: "Leads",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProjectNotes_Leads_SourceLeadId",
                table: "ProjectNotes");

            migrationBuilder.DropTable(
                name: "LeadNotes");

            migrationBuilder.DropIndex(
                name: "IX_ProjectNotes_SourceLeadId",
                table: "ProjectNotes");

            migrationBuilder.DropColumn(
                name: "SourceLeadId",
                table: "ProjectNotes");
        }
    }
}
