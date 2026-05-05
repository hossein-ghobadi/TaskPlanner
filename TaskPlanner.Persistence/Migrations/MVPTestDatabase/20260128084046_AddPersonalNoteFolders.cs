using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TaskPlanner.Persistence.Migrations.MVPTestDatabase
{
    /// <inheritdoc />
    public partial class AddPersonalNoteFolders : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "FolderId",
                table: "PersonalNotes",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "PersonalNoteFolders",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ParentFolderId = table.Column<int>(type: "int", nullable: true),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Color = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Order = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PersonalNoteFolders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PersonalNoteFolders_PersonalNoteFolders_ParentFolderId",
                        column: x => x.ParentFolderId,
                        principalTable: "PersonalNoteFolders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PersonalNotes_FolderId",
                table: "PersonalNotes",
                column: "FolderId");

            migrationBuilder.CreateIndex(
                name: "IX_PersonalNoteFolders_ParentFolderId",
                table: "PersonalNoteFolders",
                column: "ParentFolderId");

            migrationBuilder.CreateIndex(
                name: "IX_PersonalNoteFolders_UserId",
                table: "PersonalNoteFolders",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_PersonalNotes_PersonalNoteFolders_FolderId",
                table: "PersonalNotes",
                column: "FolderId",
                principalTable: "PersonalNoteFolders",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PersonalNotes_PersonalNoteFolders_FolderId",
                table: "PersonalNotes");

            migrationBuilder.DropTable(
                name: "PersonalNoteFolders");

            migrationBuilder.DropIndex(
                name: "IX_PersonalNotes_FolderId",
                table: "PersonalNotes");

            migrationBuilder.DropColumn(
                name: "FolderId",
                table: "PersonalNotes");
        }
    }
}
