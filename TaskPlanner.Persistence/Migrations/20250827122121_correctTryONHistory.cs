using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TaskPlanner.Persistence.Migrations.MVPTestDatabase
{
    /// <inheritdoc />
    public partial class correctTryONHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "OutputUrl",
                table: "TryOnHistories",
                newName: "ResultUrl");

            migrationBuilder.AlterColumn<int>(
                name: "UserPhotoId",
                table: "TryOnHistories",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ResultUrl",
                table: "TryOnHistories",
                newName: "OutputUrl");

            migrationBuilder.AlterColumn<int>(
                name: "UserPhotoId",
                table: "TryOnHistories",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);
        }
    }
}
