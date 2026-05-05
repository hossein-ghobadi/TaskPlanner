using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TaskPlanner.Persistence.Migrations.MVPTestDatabase
{
    /// <inheritdoc />
    public partial class SeedProjectIssueTypes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Epic - همیشه باید وجود داشته باشد (BaseType = 1)
            // Note: Level column will be added in migration 20251126123346_ProjectIssueTypeLevel
            // and values will be set in migration 20251126124957_UpdateSeedProjectIssueTypes
            migrationBuilder.Sql(@"
-- Epic
INSERT INTO ProjectIssueTypes (ProjectId, Name, Description, Icon, Color, [Order], BaseType, IsCustom, CanAddToSprint, CanHaveChildren, IncludeInReports, CreatedByUserId, CreatedAt)
SELECT p.Id, N'اپیک', N'نوع پیش‌فرض اپیک', N'📦', '#8B5CF6', 0, 1, 0, 0, 1, 1, p.CreatorUserId, GETUTCDATE()
FROM Projects p
WHERE NOT EXISTS (
    SELECT 1 FROM ProjectIssueTypes pit WHERE pit.ProjectId = p.Id AND pit.BaseType = 1 AND pit.IsCustom = 0
);

-- Task - همیشه باید وجود داشته باشد (BaseType = 3)
INSERT INTO ProjectIssueTypes (ProjectId, Name, Description, Icon, Color, [Order], BaseType, IsCustom, CanAddToSprint, CanHaveChildren, IncludeInReports, CreatedByUserId, CreatedAt)
SELECT p.Id, N'تسک', N'نوع پیش‌فرض تسک', N'✅', '#3B82F6', 1, 3, 0, 1, 1, 1, p.CreatorUserId, GETUTCDATE()
FROM Projects p
WHERE NOT EXISTS (
    SELECT 1 FROM ProjectIssueTypes pit WHERE pit.ProjectId = p.Id AND pit.BaseType = 3 AND pit.IsCustom = 0
);

-- Subtask - همیشه باید وجود داشته باشد (BaseType = 4)
INSERT INTO ProjectIssueTypes (ProjectId, Name, Description, Icon, Color, [Order], BaseType, IsCustom, CanAddToSprint, CanHaveChildren, IncludeInReports, CreatedByUserId, CreatedAt)
SELECT p.Id, N'زیرتسک', N'نوع پیش‌فرض زیرتسک', N'🔹', '#06B6D4', 2, 4, 0, 0, 0, 1, p.CreatorUserId, GETUTCDATE()
FROM Projects p
WHERE NOT EXISTS (
    SELECT 1 FROM ProjectIssueTypes pit WHERE pit.ProjectId = p.Id AND pit.BaseType = 4 AND pit.IsCustom = 0
);

-- به‌روزرسانی TaskItems موجود برای اتصال به ProjectIssueTypes
UPDATE ti
SET ProjectIssueTypeId = pit.Id
FROM TaskItems ti
JOIN ProjectIssueTypes pit ON pit.ProjectId = ti.ProjectId
WHERE ti.ProjectIssueTypeId IS NULL
  AND (
        (ti.IssueType = 1 AND pit.BaseType = 1) OR  -- Epic
        (ti.IssueType = 3 AND pit.BaseType = 3) OR  -- Task
        (ti.IssueType = 4 AND pit.BaseType = 4) OR  -- Subtask
        (ti.IssueType = 2 AND pit.BaseType = 2) OR  -- Story
        (ti.IssueType = 5 AND pit.BaseType = 5)     -- Bug
      );
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
UPDATE TaskItems SET ProjectIssueTypeId = NULL;
DELETE FROM ProjectIssueTypes WHERE IsCustom = 0;
");
        }
    }
}
