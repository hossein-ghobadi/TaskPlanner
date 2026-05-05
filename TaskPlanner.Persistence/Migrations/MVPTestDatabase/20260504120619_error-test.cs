using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TaskPlanner.Persistence.Migrations.MVPTestDatabase
{
    /// <inheritdoc />
    public partial class errortest : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ReplyToMessageId",
                table: "ProjectChatMessages",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Leads",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CompanyName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ContactName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Phone = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Source = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    OwnerUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ConvertedProjectId = table.Column<int>(type: "int", nullable: true),
                    ConvertedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Leads", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Leads_Projects_ConvertedProjectId",
                        column: x => x.ConvertedProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "ProjectChatMessageAttachments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProjectChatMessageId = table.Column<int>(type: "int", nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    FilePath = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    FileType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    FileSize = table.Column<long>(type: "bigint", nullable: false),
                    MimeType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    UploadedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectChatMessageAttachments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProjectChatMessageAttachments_ProjectChatMessages_ProjectChatMessageId",
                        column: x => x.ProjectChatMessageId,
                        principalTable: "ProjectChatMessages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LeadInvitation",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LeadId = table.Column<int>(type: "int", nullable: false),
                    InviterId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    InviteeId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    InviteePhone = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ResponseMessage = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RespondedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LeadInvitation", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LeadInvitation_Leads_LeadId",
                        column: x => x.LeadId,
                        principalTable: "Leads",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LeadMember",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LeadId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    AddedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LeadMember", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LeadMember_Leads_LeadId",
                        column: x => x.LeadId,
                        principalTable: "Leads",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProjectChatMessages_ReplyToMessageId",
                table: "ProjectChatMessages",
                column: "ReplyToMessageId");

            migrationBuilder.CreateIndex(
                name: "IX_LeadInvitation_LeadId_Status",
                table: "LeadInvitation",
                columns: new[] { "LeadId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_LeadMember_LeadId_UserId",
                table: "LeadMember",
                columns: new[] { "LeadId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LeadMember_UserId",
                table: "LeadMember",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Leads_ConvertedProjectId",
                table: "Leads",
                column: "ConvertedProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_Leads_OwnerUserId",
                table: "Leads",
                column: "OwnerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Leads_Status",
                table: "Leads",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectChatMessageAttachments_ProjectChatMessageId",
                table: "ProjectChatMessageAttachments",
                column: "ProjectChatMessageId");

            migrationBuilder.AddForeignKey(
                name: "FK_ProjectChatMessages_ProjectChatMessages_ReplyToMessageId",
                table: "ProjectChatMessages",
                column: "ReplyToMessageId",
                principalTable: "ProjectChatMessages",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProjectChatMessages_ProjectChatMessages_ReplyToMessageId",
                table: "ProjectChatMessages");

            migrationBuilder.DropTable(
                name: "LeadInvitation");

            migrationBuilder.DropTable(
                name: "LeadMember");

            migrationBuilder.DropTable(
                name: "ProjectChatMessageAttachments");

            migrationBuilder.DropTable(
                name: "Leads");

            migrationBuilder.DropIndex(
                name: "IX_ProjectChatMessages_ReplyToMessageId",
                table: "ProjectChatMessages");

            migrationBuilder.DropColumn(
                name: "ReplyToMessageId",
                table: "ProjectChatMessages");
        }
    }
}
