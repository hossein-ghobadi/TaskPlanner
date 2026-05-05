using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TaskPlanner.Persistence.Migrations.MVPTestDatabase
{
    /// <inheritdoc />
    public partial class AddTryOnHistoryNavs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_TryOnHistories_OutfitStyleId",
                table: "TryOnHistories",
                column: "OutfitStyleId");

            migrationBuilder.CreateIndex(
                name: "IX_TryOnHistories_UserPhotoId",
                table: "TryOnHistories",
                column: "UserPhotoId");

            migrationBuilder.AddForeignKey(
                name: "FK_TryOnHistories_OutfitStyles_OutfitStyleId",
                table: "TryOnHistories",
                column: "OutfitStyleId",
                principalTable: "OutfitStyles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_TryOnHistories_UserPhotos_UserPhotoId",
                table: "TryOnHistories",
                column: "UserPhotoId",
                principalTable: "UserPhotos",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TryOnHistories_OutfitStyles_OutfitStyleId",
                table: "TryOnHistories");

            migrationBuilder.DropForeignKey(
                name: "FK_TryOnHistories_UserPhotos_UserPhotoId",
                table: "TryOnHistories");

            migrationBuilder.DropIndex(
                name: "IX_TryOnHistories_OutfitStyleId",
                table: "TryOnHistories");

            migrationBuilder.DropIndex(
                name: "IX_TryOnHistories_UserPhotoId",
                table: "TryOnHistories");
        }
    }
}
