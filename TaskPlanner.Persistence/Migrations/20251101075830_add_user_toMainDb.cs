using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TaskPlanner.Persistence.Migrations.MVPTestDatabase
{
    /// <inheritdoc />
    public partial class add_user_toMainDb : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FavoriteOutfitStyles");

            migrationBuilder.DropTable(
                name: "TryOnHistories");

            migrationBuilder.DropTable(
                name: "UserStyleItems");

            migrationBuilder.DropTable(
                name: "OutfitStyles");

            migrationBuilder.DropTable(
                name: "UserStyles");

            migrationBuilder.DropTable(
                name: "UserPhotos");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "OutfitStyles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ContentType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ImageUrl = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OutfitStyles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UserPhotos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ContentType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FilePath = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserPhotos", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FavoriteOutfitStyles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OutfitStyleId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FavoriteOutfitStyles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FavoriteOutfitStyles_OutfitStyles_OutfitStyleId",
                        column: x => x.OutfitStyleId,
                        principalTable: "OutfitStyles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TryOnHistories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OutfitStyleId = table.Column<int>(type: "int", nullable: true),
                    UserPhotoId = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    OrderId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ResultUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TryOnHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TryOnHistories_OutfitStyles_OutfitStyleId",
                        column: x => x.OutfitStyleId,
                        principalTable: "OutfitStyles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_TryOnHistories_UserPhotos_UserPhotoId",
                        column: x => x.UserPhotoId,
                        principalTable: "UserPhotos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "UserStyles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserPhotoId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserStyles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserStyles_UserPhotos_UserPhotoId",
                        column: x => x.UserPhotoId,
                        principalTable: "UserPhotos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "UserStyleItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OutfitStyleId = table.Column<int>(type: "int", nullable: false),
                    UserStyleId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserStyleItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserStyleItems_OutfitStyles_OutfitStyleId",
                        column: x => x.OutfitStyleId,
                        principalTable: "OutfitStyles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UserStyleItems_UserStyles_UserStyleId",
                        column: x => x.UserStyleId,
                        principalTable: "UserStyles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FavoriteOutfitStyles_OutfitStyleId",
                table: "FavoriteOutfitStyles",
                column: "OutfitStyleId");

            migrationBuilder.CreateIndex(
                name: "IX_FavoriteOutfitStyles_UserId_OutfitStyleId",
                table: "FavoriteOutfitStyles",
                columns: new[] { "UserId", "OutfitStyleId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TryOnHistories_OutfitStyleId",
                table: "TryOnHistories",
                column: "OutfitStyleId");

            migrationBuilder.CreateIndex(
                name: "IX_TryOnHistories_UserId_UserPhotoId_OutfitStyleId_CreatedAt",
                table: "TryOnHistories",
                columns: new[] { "UserId", "UserPhotoId", "OutfitStyleId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_TryOnHistories_UserPhotoId",
                table: "TryOnHistories",
                column: "UserPhotoId");

            migrationBuilder.CreateIndex(
                name: "IX_UserStyleItems_OutfitStyleId",
                table: "UserStyleItems",
                column: "OutfitStyleId");

            migrationBuilder.CreateIndex(
                name: "IX_UserStyleItems_UserStyleId",
                table: "UserStyleItems",
                column: "UserStyleId");

            migrationBuilder.CreateIndex(
                name: "IX_UserStyles_UserPhotoId",
                table: "UserStyles",
                column: "UserPhotoId");
        }
    }
}
