-- افزودن ستون Tags به جدول ProjectImageGalleries (برای دسته‌بندی و فیلتر)
-- در صورت وجود قبلی ستون، این اسکریپت خطا می‌دهد؛ در آن صورت خط ALTER را حذف یا شرطی کنید.

IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'dbo.ProjectImageGalleries') AND name = 'Tags'
)
BEGIN
    ALTER TABLE [dbo].[ProjectImageGalleries]
    ADD [Tags] nvarchar(500) NULL;
END
GO
