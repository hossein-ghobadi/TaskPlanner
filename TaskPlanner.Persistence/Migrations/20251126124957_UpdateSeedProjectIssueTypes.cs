using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TaskPlanner.Persistence.Migrations.MVPTestDatabase
{
    /// <inheritdoc />
    public partial class UpdateSeedProjectIssueTypes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // به‌روزرسانی Level برای رکوردهای موجود
            migrationBuilder.Sql(@"
-- به‌روزرسانی Level برای Epic (BaseType = 1)
UPDATE ProjectIssueTypes 
SET Level = 1 
WHERE BaseType = 1 AND Level != 1;

-- به‌روزرسانی Level برای Task (BaseType = 3)
UPDATE ProjectIssueTypes 
SET Level = 2 
WHERE BaseType = 3 AND Level != 2;

-- به‌روزرسانی Level برای Subtask (BaseType = 4)
UPDATE ProjectIssueTypes 
SET Level = 3 
WHERE BaseType = 4 AND Level != 3;

-- به‌روزرسانی Level برای Story-level types (Story, Bug و انواع سفارشی)
UPDATE ProjectIssueTypes 
SET Level = 2 
WHERE BaseType IN (2, 5) AND Level != 2;
");

            // ایجاد Epic, Task, Subtask برای پروژه‌هایی که ندارند
            migrationBuilder.Sql(@"
-- Epic - همیشه باید وجود داشته باشد (Level = 1)
INSERT INTO ProjectIssueTypes (ProjectId, Name, Description, Icon, Color, [Order], BaseType, IsCustom, CanAddToSprint, CanHaveChildren, IncludeInReports, Level, CreatedByUserId, CreatedAt)
SELECT p.Id, N'اپیک', N'نوع پیش‌فرض اپیک', N'📦', '#8B5CF6', 0, 1, 0, 0, 1, 1, 1, p.CreatorUserId, GETUTCDATE()
FROM Projects p
WHERE NOT EXISTS (
    SELECT 1 FROM ProjectIssueTypes pit WHERE pit.ProjectId = p.Id AND pit.Level = 1
);

-- Task - همیشه باید وجود داشته باشد (Level = 2, StoryLevel)
INSERT INTO ProjectIssueTypes (ProjectId, Name, Description, Icon, Color, [Order], BaseType, IsCustom, CanAddToSprint, CanHaveChildren, IncludeInReports, Level, CreatedByUserId, CreatedAt)
SELECT p.Id, N'تسک', N'نوع پیش‌فرض تسک', N'✅', '#3B82F6', 1, 3, 0, 1, 1, 1, 2, p.CreatorUserId, GETUTCDATE()
FROM Projects p
WHERE NOT EXISTS (
    SELECT 1 FROM ProjectIssueTypes pit WHERE pit.ProjectId = p.Id AND pit.Level = 2 AND pit.BaseType = 3
);

-- Subtask - همیشه باید وجود داشته باشد (Level = 3)
INSERT INTO ProjectIssueTypes (ProjectId, Name, Description, Icon, Color, [Order], BaseType, IsCustom, CanAddToSprint, CanHaveChildren, IncludeInReports, Level, CreatedByUserId, CreatedAt)
SELECT p.Id, N'زیرتسک', N'نوع پیش‌فرض زیرتسک', N'🔹', '#06B6D4', 2, 4, 0, 0, 0, 1, 3, p.CreatorUserId, GETUTCDATE()
FROM Projects p
WHERE NOT EXISTS (
    SELECT 1 FROM ProjectIssueTypes pit WHERE pit.ProjectId = p.Id AND pit.Level = 3
);
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
