using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TaskPlanner.Persistence.Migrations.MVPTestDatabase
{
    /// <inheritdoc />
    public partial class add_Task_planning : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TryOnHistories_OutfitStyles_OutfitStyleId",
                table: "TryOnHistories");

            migrationBuilder.AlterColumn<int>(
                name: "OutfitStyleId",
                table: "TryOnHistories",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.CreateTable(
                name: "TaskCategories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ParentId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaskCategories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TaskCategories_TaskCategories_ParentId",
                        column: x => x.ParentId,
                        principalTable: "TaskCategories",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "TaskItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DueDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CategoryId = table.Column<int>(type: "int", nullable: false),
                    IsCompleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaskItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TaskItems_TaskCategories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "TaskCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TaskCategories_ParentId",
                table: "TaskCategories",
                column: "ParentId");

            migrationBuilder.CreateIndex(
                name: "IX_TaskItems_CategoryId",
                table: "TaskItems",
                column: "CategoryId");

            migrationBuilder.AddForeignKey(
                name: "FK_TryOnHistories_OutfitStyles_OutfitStyleId",
                table: "TryOnHistories",
                column: "OutfitStyleId",
                principalTable: "OutfitStyles",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TryOnHistories_OutfitStyles_OutfitStyleId",
                table: "TryOnHistories");

            migrationBuilder.DropTable(
                name: "TaskItems");

            migrationBuilder.DropTable(
                name: "TaskCategories");

            migrationBuilder.AlterColumn<int>(
                name: "OutfitStyleId",
                table: "TryOnHistories",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_TryOnHistories_OutfitStyles_OutfitStyleId",
                table: "TryOnHistories",
                column: "OutfitStyleId",
                principalTable: "OutfitStyles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
