using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TaskPlanner.Persistence.Migrations.MVPTestDatabase
{
    /// <inheritdoc />
    public partial class AddProjectNoteFolders : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "FolderId",
                table: "ProjectNotes",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ProjectNoteFolders",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ParentFolderId = table.Column<int>(type: "int", nullable: true),
                    ProjectId = table.Column<int>(type: "int", nullable: false),
                    CreatorUserId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Color = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Order = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectNoteFolders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProjectNoteFolders_ProjectNoteFolders_ParentFolderId",
                        column: x => x.ParentFolderId,
                        principalTable: "ProjectNoteFolders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProjectNoteFolders_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProjectNotes_FolderId",
                table: "ProjectNotes",
                column: "FolderId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectNoteFolders_ParentFolderId",
                table: "ProjectNoteFolders",
                column: "ParentFolderId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectNoteFolders_ProjectId",
                table: "ProjectNoteFolders",
                column: "ProjectId");

            migrationBuilder.AddForeignKey(
                name: "FK_ProjectNotes_ProjectNoteFolders_FolderId",
                table: "ProjectNotes",
                column: "FolderId",
                principalTable: "ProjectNoteFolders",
                principalColumn: "Id",
                onDelete: ReferentialAction.NoAction);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProjectNotes_ProjectNoteFolders_FolderId",
                table: "ProjectNotes");

            migrationBuilder.DropTable(
                name: "ProjectNoteFolders");

            migrationBuilder.DropIndex(
                name: "IX_ProjectNotes_FolderId",
                table: "ProjectNotes");

            migrationBuilder.DropColumn(
                name: "FolderId",
                table: "ProjectNotes");
        }
    }
}
