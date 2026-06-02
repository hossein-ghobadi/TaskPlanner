using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TaskPlanner.Persistence.Migrations.MVPTestDatabase
{
    /// <inheritdoc />
    public partial class AddBaleSenderDetailFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "ExternalIsChannelSender",
                table: "ProjectChatMessages",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "ExternalSenderFirstName",
                table: "ProjectChatMessages",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExternalSenderLastName",
                table: "ProjectChatMessages",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExternalSenderPhone",
                table: "ProjectChatMessages",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ExternalIsChannelSender",
                table: "ProjectChatMessages");

            migrationBuilder.DropColumn(
                name: "ExternalSenderFirstName",
                table: "ProjectChatMessages");

            migrationBuilder.DropColumn(
                name: "ExternalSenderLastName",
                table: "ProjectChatMessages");

            migrationBuilder.DropColumn(
                name: "ExternalSenderPhone",
                table: "ProjectChatMessages");
        }
    }
}
