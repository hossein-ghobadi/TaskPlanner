using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tabloyar.Persistence.Migrations.MVPTestDatabase
{
    /// <summary>
    /// Migration برای تبدیل TaskItem به Issue Model مثل Jira
    /// شامل: IssueType, IssueKey, StoryPoints, Time Tracking
    /// </summary>
    public partial class AddJiraLikeIssueModel : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. تغییر نام ستون‌های Parent-Child
            migrationBuilder.RenameColumn(
                name: "ParentId",
                table: "TaskItems",
                newName: "ParentTaskId");

            migrationBuilder.RenameIndex(
                name: "IX_TaskItems_ParentId",
                table: "TaskItems",
                newName: "IX_TaskItems_ParentTaskId");

            // 2. تبدیل CategoryId به nullable
            migrationBuilder.AlterColumn<int>(
                name: "CategoryId",
                table: "TaskItems",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            // 3. اضافه کردن ستون IssueType
            migrationBuilder.AddColumn<int>(
                name: "IssueType",
                table: "TaskItems",
                type: "int",
                nullable: false,
                defaultValue: 3); // Task = 3

            // 4. اضافه کردن ستون IssueKey
            migrationBuilder.AddColumn<string>(
                name: "IssueKey",
                table: "TaskItems",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            // 5. اضافه کردن Story Points
            migrationBuilder.AddColumn<int>(
                name: "StoryPoints",
                table: "TaskItems",
                type: "int",
                nullable: true);

            // 6. اضافه کردن Time Tracking fields
            migrationBuilder.AddColumn<decimal>(
                name: "OriginalEstimateHours",
                table: "TaskItems",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TimeSpentHours",
                table: "TaskItems",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "RemainingTimeHours",
                table: "TaskItems",
                type: "decimal(18,2)",
                nullable: true);

            // 7. اضافه کردن Audit Fields
            migrationBuilder.AddColumn<string>(
                name: "CreatedByUserId",
                table: "TaskItems",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "TaskItems",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "GETUTCDATE()");

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "TaskItems",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "GETUTCDATE()");

            // 8. تغییرات در جدول Project
            migrationBuilder.AddColumn<string>(
                name: "IssueKeyPrefix",
                table: "Projects",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "PROJ");

            migrationBuilder.AddColumn<int>(
                name: "LastIssueNumber",
                table: "Projects",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "Projects",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "GETUTCDATE()");

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "Projects",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "GETUTCDATE()");

            // 9. ایجاد Index‌ها
            migrationBuilder.CreateIndex(
                name: "IX_TaskItems_IssueKey",
                table: "TaskItems",
                column: "IssueKey",
                unique: true,
                filter: "[IssueKey] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_TaskItems_ProjectId_IssueType",
                table: "TaskItems",
                columns: new[] { "ProjectId", "IssueType" });

            migrationBuilder.CreateIndex(
                name: "IX_Projects_IssueKeyPrefix",
                table: "Projects",
                column: "IssueKeyPrefix");

            // 10. Data Migration: تبدیل TaskItem‌های موجود
            // تشخیص IssueType بر اساس ParentTaskId
            migrationBuilder.Sql(@"
                UPDATE TaskItems 
                SET IssueType = CASE 
                    WHEN ParentTaskId IS NOT NULL THEN 4  -- Subtask
                    ELSE 3  -- Task
                END
                WHERE IssueType = 3;  -- فقط آنهایی که default دارند
            ");

            // 11. Generate کردن IssueKey برای TaskItem‌های موجود
            migrationBuilder.Sql(@"
                -- Generate IssueKey برای تسک‌های موجود
                WITH NumberedTasks AS (
                    SELECT 
                        Id,
                        ProjectId,
                        ROW_NUMBER() OVER (PARTITION BY ProjectId ORDER BY Id) as RowNum
                    FROM TaskItems
                    WHERE IssueKey IS NULL
                )
                UPDATE t
                SET 
                    t.IssueKey = p.IssueKeyPrefix + '-' + CAST(nt.RowNum AS NVARCHAR(10)),
                    t.CreatedAt = ISNULL(t.StartDate, GETUTCDATE()),
                    t.UpdatedAt = GETUTCDATE()
                FROM TaskItems t
                INNER JOIN NumberedTasks nt ON t.Id = nt.Id
                INNER JOIN Projects p ON t.ProjectId = p.ProjectId;

                -- به‌روزرسانی LastIssueNumber در Projects
                UPDATE p
                SET p.LastIssueNumber = (
                    SELECT COUNT(*) 
                    FROM TaskItems t 
                    WHERE t.ProjectId = p.Id
                )
                FROM Projects p;
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // حذف Index‌ها
            migrationBuilder.DropIndex(
                name: "IX_TaskItems_IssueKey",
                table: "TaskItems");

            migrationBuilder.DropIndex(
                name: "IX_TaskItems_ProjectId_IssueType",
                table: "TaskItems");

            migrationBuilder.DropIndex(
                name: "IX_Projects_IssueKeyPrefix",
                table: "Projects");

            // حذف ستون‌های جدید از TaskItems
            migrationBuilder.DropColumn(
                name: "IssueType",
                table: "TaskItems");

            migrationBuilder.DropColumn(
                name: "IssueKey",
                table: "TaskItems");

            migrationBuilder.DropColumn(
                name: "StoryPoints",
                table: "TaskItems");

            migrationBuilder.DropColumn(
                name: "OriginalEstimateHours",
                table: "TaskItems");

            migrationBuilder.DropColumn(
                name: "TimeSpentHours",
                table: "TaskItems");

            migrationBuilder.DropColumn(
                name: "RemainingTimeHours",
                table: "TaskItems");

            migrationBuilder.DropColumn(
                name: "CreatedByUserId",
                table: "TaskItems");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "TaskItems");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "TaskItems");

            // حذف ستون‌های جدید از Projects
            migrationBuilder.DropColumn(
                name: "IssueKeyPrefix",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "LastIssueNumber",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "Projects");

            // برگرداندن CategoryId به non-nullable
            migrationBuilder.AlterColumn<int>(
                name: "CategoryId",
                table: "TaskItems",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            // برگرداندن نام ستون‌ها
            migrationBuilder.RenameColumn(
                name: "ParentTaskId",
                table: "TaskItems",
                newName: "ParentId");

            migrationBuilder.RenameIndex(
                name: "IX_TaskItems_ParentTaskId",
                table: "TaskItems",
                newName: "IX_TaskItems_ParentId");
        }
    }
}

