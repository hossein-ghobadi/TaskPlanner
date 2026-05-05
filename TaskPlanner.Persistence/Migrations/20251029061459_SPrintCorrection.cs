using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TaskPlanner.Persistence.Migrations.MVPTestDatabase
{
    /// <inheritdoc />
    public partial class SPrintCorrection : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TaskItems_User_AssignedUserId",
                table: "TaskItems");

            migrationBuilder.DropForeignKey(
                name: "FK_TaskItems_WorkflowStatuses_WorkflowStatusId",
                table: "TaskItems");

            migrationBuilder.AddColumn<bool>(
                name: "IsDefault",
                table: "WorkflowStatuses",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsFinal",
                table: "WorkflowStatuses",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "WorkflowStatuses",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<int>(
                name: "StatusId",
                table: "TaskItems",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "WorkflowTransitions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    FromStatusId = table.Column<int>(type: "int", nullable: false),
                    ToStatusId = table.Column<int>(type: "int", nullable: false),
                    ProjectId = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    Order = table.Column<int>(type: "int", nullable: false),
                    RequiresApproval = table.Column<bool>(type: "bit", nullable: false),
                    OnlyAssigneeCanTransition = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkflowTransitions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkflowTransitions_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_WorkflowTransitions_WorkflowStatuses_FromStatusId",
                        column: x => x.FromStatusId,
                        principalTable: "WorkflowStatuses",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_WorkflowTransitions_WorkflowStatuses_ToStatusId",
                        column: x => x.ToStatusId,
                        principalTable: "WorkflowStatuses",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "IssueStatusHistories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TaskId = table.Column<int>(type: "int", nullable: false),
                    FromStatusId = table.Column<int>(type: "int", nullable: true),
                    ToStatusId = table.Column<int>(type: "int", nullable: false),
                    TransitionId = table.Column<int>(type: "int", nullable: true),
                    ChangedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    ChangeReason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ChangedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IssueStatusHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IssueStatusHistories_TaskItems_TaskId",
                        column: x => x.TaskId,
                        principalTable: "TaskItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_IssueStatusHistories_WorkflowStatuses_FromStatusId",
                        column: x => x.FromStatusId,
                        principalTable: "WorkflowStatuses",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_IssueStatusHistories_WorkflowStatuses_ToStatusId",
                        column: x => x.ToStatusId,
                        principalTable: "WorkflowStatuses",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_IssueStatusHistories_WorkflowTransitions_TransitionId",
                        column: x => x.TransitionId,
                        principalTable: "WorkflowTransitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TaskItems_StatusId",
                table: "TaskItems",
                column: "StatusId");

            migrationBuilder.CreateIndex(
                name: "IX_IssueStatusHistories_ChangedAt",
                table: "IssueStatusHistories",
                column: "ChangedAt");

            migrationBuilder.CreateIndex(
                name: "IX_IssueStatusHistories_FromStatusId",
                table: "IssueStatusHistories",
                column: "FromStatusId");

            migrationBuilder.CreateIndex(
                name: "IX_IssueStatusHistories_TaskId",
                table: "IssueStatusHistories",
                column: "TaskId");

            migrationBuilder.CreateIndex(
                name: "IX_IssueStatusHistories_ToStatusId",
                table: "IssueStatusHistories",
                column: "ToStatusId");

            migrationBuilder.CreateIndex(
                name: "IX_IssueStatusHistories_TransitionId",
                table: "IssueStatusHistories",
                column: "TransitionId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowTransitions_FromStatusId_ToStatusId_ProjectId",
                table: "WorkflowTransitions",
                columns: new[] { "FromStatusId", "ToStatusId", "ProjectId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowTransitions_ProjectId",
                table: "WorkflowTransitions",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowTransitions_ToStatusId",
                table: "WorkflowTransitions",
                column: "ToStatusId");

            migrationBuilder.AddForeignKey(
                name: "FK_TaskItems_User_AssignedUserId",
                table: "TaskItems",
                column: "AssignedUserId",
                principalTable: "User",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_TaskItems_WorkflowStatuses_StatusId",
                table: "TaskItems",
                column: "StatusId",
                principalTable: "WorkflowStatuses",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_TaskItems_WorkflowStatuses_WorkflowStatusId",
                table: "TaskItems",
                column: "WorkflowStatusId",
                principalTable: "WorkflowStatuses",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TaskItems_User_AssignedUserId",
                table: "TaskItems");

            migrationBuilder.DropForeignKey(
                name: "FK_TaskItems_WorkflowStatuses_StatusId",
                table: "TaskItems");

            migrationBuilder.DropForeignKey(
                name: "FK_TaskItems_WorkflowStatuses_WorkflowStatusId",
                table: "TaskItems");

            migrationBuilder.DropTable(
                name: "IssueStatusHistories");

            migrationBuilder.DropTable(
                name: "WorkflowTransitions");

            migrationBuilder.DropIndex(
                name: "IX_TaskItems_StatusId",
                table: "TaskItems");

            migrationBuilder.DropColumn(
                name: "IsDefault",
                table: "WorkflowStatuses");

            migrationBuilder.DropColumn(
                name: "IsFinal",
                table: "WorkflowStatuses");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "WorkflowStatuses");

            migrationBuilder.DropColumn(
                name: "StatusId",
                table: "TaskItems");

            migrationBuilder.AddForeignKey(
                name: "FK_TaskItems_User_AssignedUserId",
                table: "TaskItems",
                column: "AssignedUserId",
                principalTable: "User",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_TaskItems_WorkflowStatuses_WorkflowStatusId",
                table: "TaskItems",
                column: "WorkflowStatusId",
                principalTable: "WorkflowStatuses",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
