using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TaskPlanner.Persistence.Migrations.MVPTestDatabase
{
    /// <inheritdoc />
    public partial class AddBaleSenderAndMediaFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "ExternalSenderBaleId",
                table: "ProjectChatMessages",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExternalSenderPhotoPath",
                table: "ProjectChatMessages",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExternalSenderUsername",
                table: "ProjectChatMessages",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ExternalSenderBaleId",
                table: "ProjectChatMessages");

            migrationBuilder.DropColumn(
                name: "ExternalSenderPhotoPath",
                table: "ProjectChatMessages");

            migrationBuilder.DropColumn(
                name: "ExternalSenderUsername",
                table: "ProjectChatMessages");
        }
    }
}
