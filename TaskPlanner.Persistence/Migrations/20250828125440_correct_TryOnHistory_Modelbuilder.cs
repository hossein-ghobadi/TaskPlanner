using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TaskPlanner.Persistence.Migrations.MVPTestDatabase
{
    /// <inheritdoc />
    public partial class correct_TryOnHistory_Modelbuilder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TryOnHistories_UserPhotos_UserPhotoId",
                table: "TryOnHistories");

            migrationBuilder.AddForeignKey(
                name: "FK_TryOnHistories_UserPhotos_UserPhotoId",
                table: "TryOnHistories",
                column: "UserPhotoId",
                principalTable: "UserPhotos",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TryOnHistories_UserPhotos_UserPhotoId",
                table: "TryOnHistories");

            migrationBuilder.AddForeignKey(
                name: "FK_TryOnHistories_UserPhotos_UserPhotoId",
                table: "TryOnHistories",
                column: "UserPhotoId",
                principalTable: "UserPhotos",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
