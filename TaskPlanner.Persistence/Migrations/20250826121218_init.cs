using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TaskPlanner.Persistence.Migrations.MVPTestDatabase
{
    /// <inheritdoc />
    public partial class init : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "OutfitStyles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ImageUrl = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ContentType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OutfitStyles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TryOnHistories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    OutfitStyleId = table.Column<int>(type: "int", nullable: false),
                    UserPhotoId = table.Column<int>(type: "int", nullable: false),
                    OrderId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OutputUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TryOnHistories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UserPhotos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FilePath = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ContentType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
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
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    OutfitStyleId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
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
                name: "UserStyles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UserPhotoId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
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
                    UserStyleId = table.Column<int>(type: "int", nullable: false),
                    OutfitStyleId = table.Column<int>(type: "int", nullable: false)
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
                name: "IX_TryOnHistories_UserId_UserPhotoId_OutfitStyleId_CreatedAt",
                table: "TryOnHistories",
                columns: new[] { "UserId", "UserPhotoId", "OutfitStyleId", "CreatedAt" });

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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
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
    }
}
