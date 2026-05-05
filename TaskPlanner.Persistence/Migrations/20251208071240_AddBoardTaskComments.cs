using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TaskPlanner.Persistence.Migrations.MVPTestDatabase
{
    /// <inheritdoc />
    public partial class AddBoardTaskComments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ایجاد جدول BoardTaskComments
            migrationBuilder.CreateTable(
                name: "BoardTaskComments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Message = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BoardTaskId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    UserName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsEdited = table.Column<bool>(type: "bit", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BoardTaskComments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BoardTaskComments_BoardTasks_BoardTaskId",
                        column: x => x.BoardTaskId,
                        principalTable: "BoardTasks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            // ایجاد جدول BoardTaskCommentAttachments
            migrationBuilder.CreateTable(
                name: "BoardTaskCommentAttachments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BoardTaskCommentId = table.Column<int>(type: "int", nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    FilePath = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    FileType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    FileSize = table.Column<long>(type: "bigint", nullable: false),
                    MimeType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    UploadedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BoardTaskCommentAttachments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BoardTaskCommentAttachments_BoardTaskComments_BoardTaskCommentId",
                        column: x => x.BoardTaskCommentId,
                        principalTable: "BoardTaskComments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BoardTaskComments_BoardTaskId",
                table: "BoardTaskComments",
                column: "BoardTaskId");

            migrationBuilder.CreateIndex(
                name: "IX_BoardTaskCommentAttachments_BoardTaskCommentId",
                table: "BoardTaskCommentAttachments",
                column: "BoardTaskCommentId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BoardTaskCommentAttachments");

            migrationBuilder.DropTable(
                name: "BoardTaskComments");
        }
    }
}
