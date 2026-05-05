using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TaskPlanner.Persistence.Migrations.MVPTestDatabase
{
    /// <inheritdoc />
    public partial class addleadmember : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_LeadInvitation_Leads_LeadId",
                table: "LeadInvitation");

            migrationBuilder.DropForeignKey(
                name: "FK_LeadMember_Leads_LeadId",
                table: "LeadMember");

            migrationBuilder.DropPrimaryKey(
                name: "PK_LeadMember",
                table: "LeadMember");

            migrationBuilder.DropPrimaryKey(
                name: "PK_LeadInvitation",
                table: "LeadInvitation");

            migrationBuilder.RenameTable(
                name: "LeadMember",
                newName: "LeadMembers");

            migrationBuilder.RenameTable(
                name: "LeadInvitation",
                newName: "LeadInvitations");

            migrationBuilder.RenameIndex(
                name: "IX_LeadMember_UserId",
                table: "LeadMembers",
                newName: "IX_LeadMembers_UserId");

            migrationBuilder.RenameIndex(
                name: "IX_LeadMember_LeadId_UserId",
                table: "LeadMembers",
                newName: "IX_LeadMembers_LeadId_UserId");

            migrationBuilder.RenameIndex(
                name: "IX_LeadInvitation_LeadId_Status",
                table: "LeadInvitations",
                newName: "IX_LeadInvitations_LeadId_Status");

            migrationBuilder.AddPrimaryKey(
                name: "PK_LeadMembers",
                table: "LeadMembers",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_LeadInvitations",
                table: "LeadInvitations",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_LeadInvitations_Leads_LeadId",
                table: "LeadInvitations",
                column: "LeadId",
                principalTable: "Leads",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_LeadMembers_Leads_LeadId",
                table: "LeadMembers",
                column: "LeadId",
                principalTable: "Leads",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_LeadInvitations_Leads_LeadId",
                table: "LeadInvitations");

            migrationBuilder.DropForeignKey(
                name: "FK_LeadMembers_Leads_LeadId",
                table: "LeadMembers");

            migrationBuilder.DropPrimaryKey(
                name: "PK_LeadMembers",
                table: "LeadMembers");

            migrationBuilder.DropPrimaryKey(
                name: "PK_LeadInvitations",
                table: "LeadInvitations");

            migrationBuilder.RenameTable(
                name: "LeadMembers",
                newName: "LeadMember");

            migrationBuilder.RenameTable(
                name: "LeadInvitations",
                newName: "LeadInvitation");

            migrationBuilder.RenameIndex(
                name: "IX_LeadMembers_UserId",
                table: "LeadMember",
                newName: "IX_LeadMember_UserId");

            migrationBuilder.RenameIndex(
                name: "IX_LeadMembers_LeadId_UserId",
                table: "LeadMember",
                newName: "IX_LeadMember_LeadId_UserId");

            migrationBuilder.RenameIndex(
                name: "IX_LeadInvitations_LeadId_Status",
                table: "LeadInvitation",
                newName: "IX_LeadInvitation_LeadId_Status");

            migrationBuilder.AddPrimaryKey(
                name: "PK_LeadMember",
                table: "LeadMember",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_LeadInvitation",
                table: "LeadInvitation",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_LeadInvitation_Leads_LeadId",
                table: "LeadInvitation",
                column: "LeadId",
                principalTable: "Leads",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_LeadMember_Leads_LeadId",
                table: "LeadMember",
                column: "LeadId",
                principalTable: "Leads",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
