using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TaskPlanner.Persistence.Migrations.MVPTestDatabase
{
    /// <inheritdoc />
    public partial class AddCrmWorkspace : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CreatedByUserId",
                table: "Leads",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CrmId",
                table: "Leads",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Crms",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    OwnerUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Crms", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CrmInvitations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CrmId = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_CrmInvitations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CrmInvitations_Crms_CrmId",
                        column: x => x.CrmId,
                        principalTable: "Crms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CrmMembers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CrmId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Role = table.Column<int>(type: "int", nullable: false),
                    AddedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CrmMembers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CrmMembers_Crms_CrmId",
                        column: x => x.CrmId,
                        principalTable: "Crms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Leads_CrmId",
                table: "Leads",
                column: "CrmId");

            migrationBuilder.CreateIndex(
                name: "IX_CrmInvitations_CrmId_Status",
                table: "CrmInvitations",
                columns: new[] { "CrmId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_CrmMembers_CrmId_UserId",
                table: "CrmMembers",
                columns: new[] { "CrmId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CrmMembers_UserId",
                table: "CrmMembers",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Crms_OwnerUserId",
                table: "Crms",
                column: "OwnerUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Leads_Crms_CrmId",
                table: "Leads",
                column: "CrmId",
                principalTable: "Crms",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Leads_Crms_CrmId",
                table: "Leads");

            migrationBuilder.DropTable(
                name: "CrmInvitations");

            migrationBuilder.DropTable(
                name: "CrmMembers");

            migrationBuilder.DropTable(
                name: "Crms");

            migrationBuilder.DropIndex(
                name: "IX_Leads_CrmId",
                table: "Leads");

            migrationBuilder.DropColumn(
                name: "CreatedByUserId",
                table: "Leads");

            migrationBuilder.DropColumn(
                name: "CrmId",
                table: "Leads");
        }
    }
}
