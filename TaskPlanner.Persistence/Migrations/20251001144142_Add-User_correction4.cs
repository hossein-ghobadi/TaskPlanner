using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TaskPlanner.Persistence.Migrations.MVPTestDatabase
{
    /// <inheritdoc />
    public partial class AddUser_correction4 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProjectMembers_Projects_ProjectId",
                table: "ProjectMembers");

            migrationBuilder.DropForeignKey(
                name: "FK_ProjectMembers_User_UserId",
                table: "ProjectMembers");

            migrationBuilder.DropForeignKey(
                name: "FK_Projects_User_CreatorUserId",
                table: "Projects");

            migrationBuilder.DropForeignKey(
                name: "FK_TaskItems_Projects_ProjectId",
                table: "TaskItems");

            migrationBuilder.DropForeignKey(
                name: "FK_TaskItems_TaskCategories_CategoryId",
                table: "TaskItems");

            migrationBuilder.DropForeignKey(
                name: "FK_TaskItems_TaskItems_ParentId",
                table: "TaskItems");

            migrationBuilder.DropForeignKey(
                name: "FK_TaskItems_User_AssignedUserId",
                table: "TaskItems");

            migrationBuilder.DropPrimaryKey(
                name: "PK_TaskItems",
                table: "TaskItems");

            migrationBuilder.DropPrimaryKey(
                name: "PK_TaskCategories",
                table: "TaskCategories");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Projects",
                table: "Projects");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ProjectMembers",
                table: "ProjectMembers");

            migrationBuilder.RenameTable(
                name: "TaskItems",
                newName: "TaskItem");

            migrationBuilder.RenameTable(
                name: "TaskCategories",
                newName: "TaskCategory");

            migrationBuilder.RenameTable(
                name: "Projects",
                newName: "Project");

            migrationBuilder.RenameTable(
                name: "ProjectMembers",
                newName: "ProjectMember");

            migrationBuilder.RenameIndex(
                name: "IX_TaskItems_ProjectId",
                table: "TaskItem",
                newName: "IX_TaskItem_ProjectId");

            migrationBuilder.RenameIndex(
                name: "IX_TaskItems_ParentId",
                table: "TaskItem",
                newName: "IX_TaskItem_ParentId");

            migrationBuilder.RenameIndex(
                name: "IX_TaskItems_CategoryId",
                table: "TaskItem",
                newName: "IX_TaskItem_CategoryId");

            migrationBuilder.RenameIndex(
                name: "IX_TaskItems_AssignedUserId",
                table: "TaskItem",
                newName: "IX_TaskItem_AssignedUserId");

            migrationBuilder.RenameIndex(
                name: "IX_Projects_CreatorUserId",
                table: "Project",
                newName: "IX_Project_CreatorUserId");

            migrationBuilder.RenameIndex(
                name: "IX_ProjectMembers_UserId",
                table: "ProjectMember",
                newName: "IX_ProjectMember_UserId");

            migrationBuilder.RenameIndex(
                name: "IX_ProjectMembers_ProjectId",
                table: "ProjectMember",
                newName: "IX_ProjectMember_ProjectId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_TaskItem",
                table: "TaskItem",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_TaskCategory",
                table: "TaskCategory",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Project",
                table: "Project",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ProjectMember",
                table: "ProjectMember",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Project_User_CreatorUserId",
                table: "Project",
                column: "CreatorUserId",
                principalTable: "User",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ProjectMember_Project_ProjectId",
                table: "ProjectMember",
                column: "ProjectId",
                principalTable: "Project",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ProjectMember_User_UserId",
                table: "ProjectMember",
                column: "UserId",
                principalTable: "User",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TaskItem_Project_ProjectId",
                table: "TaskItem",
                column: "ProjectId",
                principalTable: "Project",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_TaskItem_TaskCategory_CategoryId",
                table: "TaskItem",
                column: "CategoryId",
                principalTable: "TaskCategory",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TaskItem_TaskItem_ParentId",
                table: "TaskItem",
                column: "ParentId",
                principalTable: "TaskItem",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_TaskItem_User_AssignedUserId",
                table: "TaskItem",
                column: "AssignedUserId",
                principalTable: "User",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Project_User_CreatorUserId",
                table: "Project");

            migrationBuilder.DropForeignKey(
                name: "FK_ProjectMember_Project_ProjectId",
                table: "ProjectMember");

            migrationBuilder.DropForeignKey(
                name: "FK_ProjectMember_User_UserId",
                table: "ProjectMember");

            migrationBuilder.DropForeignKey(
                name: "FK_TaskItem_Project_ProjectId",
                table: "TaskItem");

            migrationBuilder.DropForeignKey(
                name: "FK_TaskItem_TaskCategory_CategoryId",
                table: "TaskItem");

            migrationBuilder.DropForeignKey(
                name: "FK_TaskItem_TaskItem_ParentId",
                table: "TaskItem");

            migrationBuilder.DropForeignKey(
                name: "FK_TaskItem_User_AssignedUserId",
                table: "TaskItem");

            migrationBuilder.DropPrimaryKey(
                name: "PK_TaskItem",
                table: "TaskItem");

            migrationBuilder.DropPrimaryKey(
                name: "PK_TaskCategory",
                table: "TaskCategory");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ProjectMember",
                table: "ProjectMember");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Project",
                table: "Project");

            migrationBuilder.RenameTable(
                name: "TaskItem",
                newName: "TaskItems");

            migrationBuilder.RenameTable(
                name: "TaskCategory",
                newName: "TaskCategories");

            migrationBuilder.RenameTable(
                name: "ProjectMember",
                newName: "ProjectMembers");

            migrationBuilder.RenameTable(
                name: "Project",
                newName: "Projects");

            migrationBuilder.RenameIndex(
                name: "IX_TaskItem_ProjectId",
                table: "TaskItems",
                newName: "IX_TaskItems_ProjectId");

            migrationBuilder.RenameIndex(
                name: "IX_TaskItem_ParentId",
                table: "TaskItems",
                newName: "IX_TaskItems_ParentId");

            migrationBuilder.RenameIndex(
                name: "IX_TaskItem_CategoryId",
                table: "TaskItems",
                newName: "IX_TaskItems_CategoryId");

            migrationBuilder.RenameIndex(
                name: "IX_TaskItem_AssignedUserId",
                table: "TaskItems",
                newName: "IX_TaskItems_AssignedUserId");

            migrationBuilder.RenameIndex(
                name: "IX_ProjectMember_UserId",
                table: "ProjectMembers",
                newName: "IX_ProjectMembers_UserId");

            migrationBuilder.RenameIndex(
                name: "IX_ProjectMember_ProjectId",
                table: "ProjectMembers",
                newName: "IX_ProjectMembers_ProjectId");

            migrationBuilder.RenameIndex(
                name: "IX_Project_CreatorUserId",
                table: "Projects",
                newName: "IX_Projects_CreatorUserId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_TaskItems",
                table: "TaskItems",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_TaskCategories",
                table: "TaskCategories",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ProjectMembers",
                table: "ProjectMembers",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Projects",
                table: "Projects",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ProjectMembers_Projects_ProjectId",
                table: "ProjectMembers",
                column: "ProjectId",
                principalTable: "Projects",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ProjectMembers_User_UserId",
                table: "ProjectMembers",
                column: "UserId",
                principalTable: "User",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Projects_User_CreatorUserId",
                table: "Projects",
                column: "CreatorUserId",
                principalTable: "User",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_TaskItems_Projects_ProjectId",
                table: "TaskItems",
                column: "ProjectId",
                principalTable: "Projects",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_TaskItems_TaskCategories_CategoryId",
                table: "TaskItems",
                column: "CategoryId",
                principalTable: "TaskCategories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TaskItems_TaskItems_ParentId",
                table: "TaskItems",
                column: "ParentId",
                principalTable: "TaskItems",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_TaskItems_User_AssignedUserId",
                table: "TaskItems",
                column: "AssignedUserId",
                principalTable: "User",
                principalColumn: "Id");
        }
    }
}
