using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TaskPlanner.Persistence.Migrations.MVPTestDatabase
{
    /// <inheritdoc />
    public partial class FixProjectImageGalleryCascade : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProjectImageGalleries_Projects_ProjectId",
                table: "ProjectImageGalleries");

            migrationBuilder.DropForeignKey(
                name: "FK_ProjectImageGalleryFolders_Projects_ProjectId",
                table: "ProjectImageGalleryFolders");

            migrationBuilder.AddForeignKey(
                name: "FK_ProjectImageGalleries_Projects_ProjectId",
                table: "ProjectImageGalleries",
                column: "ProjectId",
                principalTable: "Projects",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ProjectImageGalleryFolders_Projects_ProjectId",
                table: "ProjectImageGalleryFolders",
                column: "ProjectId",
                principalTable: "Projects",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProjectImageGalleries_Projects_ProjectId",
                table: "ProjectImageGalleries");

            migrationBuilder.DropForeignKey(
                name: "FK_ProjectImageGalleryFolders_Projects_ProjectId",
                table: "ProjectImageGalleryFolders");

            migrationBuilder.AddForeignKey(
                name: "FK_ProjectImageGalleries_Projects_ProjectId",
                table: "ProjectImageGalleries",
                column: "ProjectId",
                principalTable: "Projects",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ProjectImageGalleryFolders_Projects_ProjectId",
                table: "ProjectImageGalleryFolders",
                column: "ProjectId",
                principalTable: "Projects",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
