using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TaskPlanner.Persistence.Migrations.MVPTestDatabase
{
    /// <inheritdoc />
    public partial class RemoveBoardStatusIdFromBoardTasks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Check if constraint exists before dropping
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_BoardTasks_BoardStatuses_BoardStatusId')
                BEGIN
                    ALTER TABLE [BoardTasks] DROP CONSTRAINT [FK_BoardTasks_BoardStatuses_BoardStatusId];
                END
            ");

            // Check if index exists before dropping
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_BoardTasks_BoardStatusId' AND object_id = OBJECT_ID('BoardTasks'))
                BEGIN
                    DROP INDEX [IX_BoardTasks_BoardStatusId] ON [BoardTasks];
                END
            ");

            // Check if column exists before dropping
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('BoardTasks') AND name = 'BoardStatusId')
                BEGIN
                    ALTER TABLE [BoardTasks] DROP COLUMN [BoardStatusId];
                END
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "BoardStatusId",
                table: "BoardTasks",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_BoardTasks_BoardStatusId",
                table: "BoardTasks",
                column: "BoardStatusId");

            migrationBuilder.AddForeignKey(
                name: "FK_BoardTasks_BoardStatuses_BoardStatusId",
                table: "BoardTasks",
                column: "BoardStatusId",
                principalTable: "BoardStatuses",
                principalColumn: "Id");
        }
    }
}
