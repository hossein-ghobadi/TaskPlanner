-- Migration Script: به‌روزرسانی نام‌های پیش‌فرض Issue Types
-- تاریخ: 2024
-- توضیحات: تبدیل نام‌های قدیمی به نام‌های جدید
--   اپیک → ویژگی
--   تسک → کار
--   زیرتسک → کارک

BEGIN TRANSACTION;

-- به‌روزرسانی نام Epic (اپیک → ویژگی)
UPDATE ProjectIssueTypes
SET Name = N'ویژگی',
    Description = N'نوع پیش‌فرض ویژگی'
WHERE IsCustom = 0 
  AND BaseType = 1  -- IssueType.Epic
  AND Name = N'اپیک';

-- به‌روزرسانی نام Task (تسک → کار)
UPDATE ProjectIssueTypes
SET Name = N'کار',
    Description = N'نوع پیش‌فرض کار'
WHERE IsCustom = 0 
  AND BaseType = 3  -- IssueType.Task
  AND Level = 2      -- IssueTypeLevel.StoryLevel
  AND Name = N'تسک';

-- به‌روزرسانی نام Subtask (زیرتسک → کارک)
UPDATE ProjectIssueTypes
SET Name = N'کارک',
    Description = N'نوع پیش‌فرض کارک'
WHERE IsCustom = 0 
  AND BaseType = 4  -- IssueType.Subtask
  AND Name = N'زیرتسک';

-- نمایش تعداد رکوردهای به‌روزرسانی شده
SELECT 
    'Epic (ویژگی)' AS Type,
    COUNT(*) AS UpdatedCount
FROM ProjectIssueTypes
WHERE IsCustom = 0 AND BaseType = 1 AND Name = N'ویژگی'
UNION ALL
SELECT 
    'Task (کار)' AS Type,
    COUNT(*) AS UpdatedCount
FROM ProjectIssueTypes
WHERE IsCustom = 0 AND BaseType = 3 AND Level = 2 AND Name = N'کار'
UNION ALL
SELECT 
    'Subtask (کارک)' AS Type,
    COUNT(*) AS UpdatedCount
FROM ProjectIssueTypes
WHERE IsCustom = 0 AND BaseType = 4 AND Name = N'کارک';

COMMIT TRANSACTION;

-- در صورت نیاز به Rollback:
-- BEGIN TRANSACTION;
-- UPDATE ProjectIssueTypes SET Name = N'اپیک', Description = N'نوع پیش‌فرض اپیک' WHERE Name = N'ویژگی' AND IsCustom = 0 AND BaseType = 1;
-- UPDATE ProjectIssueTypes SET Name = N'تسک', Description = N'نوع پیش‌فرض تسک' WHERE Name = N'کار' AND IsCustom = 0 AND BaseType = 3 AND Level = 2;
-- UPDATE ProjectIssueTypes SET Name = N'زیرتسک', Description = N'نوع پیش‌فرض زیرتسک' WHERE Name = N'کارک' AND IsCustom = 0 AND BaseType = 4;
-- COMMIT TRANSACTION;

