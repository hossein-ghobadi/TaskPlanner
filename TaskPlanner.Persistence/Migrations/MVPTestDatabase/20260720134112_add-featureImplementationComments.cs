using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TaskPlanner.Persistence.Migrations.MVPTestDatabase
{
    /// <inheritdoc />
    public partial class addfeatureImplementationComments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FeatureImplementationComments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FeatureId = table.Column<int>(type: "int", nullable: false),
                    AuthorUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Body = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FeatureImplementationComments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FeatureImplementationComments_AspNetUsers_AuthorUserId",
                        column: x => x.AuthorUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FeatureImplementationComments_ProjectFeatures_FeatureId",
                        column: x => x.FeatureId,
                        principalTable: "ProjectFeatures",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FeatureImplementationComments_AuthorUserId",
                table: "FeatureImplementationComments",
                column: "AuthorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_FeatureImplementationComments_FeatureId",
                table: "FeatureImplementationComments",
                column: "FeatureId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FeatureImplementationComments");
        }
    }
}
