using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TaskPlanner.Persistence.Migrations.MVPTestDatabase
{
    /// <inheritdoc />
    public partial class addprojectFeature : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "FeatureId",
                table: "TaskItems",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ProjectFeatures",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProjectId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CodeReviewerUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    CreatedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectFeatures", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProjectFeatures_AspNetUsers_CodeReviewerUserId",
                        column: x => x.CodeReviewerUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ProjectFeatures_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FeatureApiContracts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FeatureId = table.Column<int>(type: "int", nullable: false),
                    Endpoint = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    HttpMethod = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    RequestDescription = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    ResponseDescription = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    ErrorResponseDescription = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    StatusCodes = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Pagination = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Filter = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Sort = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    FieldsDescription = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FeatureApiContracts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FeatureApiContracts_ProjectFeatures_FeatureId",
                        column: x => x.FeatureId,
                        principalTable: "ProjectFeatures",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FeatureBusinessRules",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FeatureId = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FeatureBusinessRules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FeatureBusinessRules_ProjectFeatures_FeatureId",
                        column: x => x.FeatureId,
                        principalTable: "ProjectFeatures",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FeatureCodeReviews",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FeatureId = table.Column<int>(type: "int", nullable: false),
                    ReviewerUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    RevieweeUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Score = table.Column<int>(type: "int", nullable: false),
                    Comment = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FeatureCodeReviews", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FeatureCodeReviews_AspNetUsers_RevieweeUserId",
                        column: x => x.RevieweeUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FeatureCodeReviews_AspNetUsers_ReviewerUserId",
                        column: x => x.ReviewerUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FeatureCodeReviews_ProjectFeatures_FeatureId",
                        column: x => x.FeatureId,
                        principalTable: "ProjectFeatures",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FeatureFunctions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FeatureId = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FeatureFunctions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FeatureFunctions_ProjectFeatures_FeatureId",
                        column: x => x.FeatureId,
                        principalTable: "ProjectFeatures",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FeaturePageStates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FeatureId = table.Column<int>(type: "int", nullable: false),
                    StateType = table.Column<int>(type: "int", nullable: false),
                    BehaviorDescription = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    HasSeparateDesign = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FeaturePageStates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FeaturePageStates_ProjectFeatures_FeatureId",
                        column: x => x.FeatureId,
                        principalTable: "ProjectFeatures",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TaskItems_FeatureId",
                table: "TaskItems",
                column: "FeatureId");

            migrationBuilder.CreateIndex(
                name: "IX_FeatureApiContracts_FeatureId",
                table: "FeatureApiContracts",
                column: "FeatureId");

            migrationBuilder.CreateIndex(
                name: "IX_FeatureBusinessRules_FeatureId",
                table: "FeatureBusinessRules",
                column: "FeatureId");

            migrationBuilder.CreateIndex(
                name: "IX_FeatureCodeReviews_FeatureId",
                table: "FeatureCodeReviews",
                column: "FeatureId");

            migrationBuilder.CreateIndex(
                name: "IX_FeatureCodeReviews_RevieweeUserId",
                table: "FeatureCodeReviews",
                column: "RevieweeUserId");

            migrationBuilder.CreateIndex(
                name: "IX_FeatureCodeReviews_ReviewerUserId",
                table: "FeatureCodeReviews",
                column: "ReviewerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_FeatureFunctions_FeatureId",
                table: "FeatureFunctions",
                column: "FeatureId");

            migrationBuilder.CreateIndex(
                name: "IX_FeaturePageStates_FeatureId_StateType",
                table: "FeaturePageStates",
                columns: new[] { "FeatureId", "StateType" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProjectFeatures_CodeReviewerUserId",
                table: "ProjectFeatures",
                column: "CodeReviewerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectFeatures_ProjectId",
                table: "ProjectFeatures",
                column: "ProjectId");

            migrationBuilder.AddForeignKey(
                name: "FK_TaskItems_ProjectFeatures_FeatureId",
                table: "TaskItems",
                column: "FeatureId",
                principalTable: "ProjectFeatures",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TaskItems_ProjectFeatures_FeatureId",
                table: "TaskItems");

            migrationBuilder.DropTable(
                name: "FeatureApiContracts");

            migrationBuilder.DropTable(
                name: "FeatureBusinessRules");

            migrationBuilder.DropTable(
                name: "FeatureCodeReviews");

            migrationBuilder.DropTable(
                name: "FeatureFunctions");

            migrationBuilder.DropTable(
                name: "FeaturePageStates");

            migrationBuilder.DropTable(
                name: "ProjectFeatures");

            migrationBuilder.DropIndex(
                name: "IX_TaskItems_FeatureId",
                table: "TaskItems");

            migrationBuilder.DropColumn(
                name: "FeatureId",
                table: "TaskItems");
        }
    }
}
