-- حذف ستون BoardStatusId از جدول BoardTasks
IF EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_BoardTasks_BoardStatuses_BoardStatusId')
BEGIN
    ALTER TABLE [BoardTasks] DROP CONSTRAINT [FK_BoardTasks_BoardStatuses_BoardStatusId];
END

IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_BoardTasks_BoardStatusId')
BEGIN
    DROP INDEX [IX_BoardTasks_BoardStatusId] ON [BoardTasks];
END

IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('BoardTasks') AND name = 'BoardStatusId')
BEGIN
    ALTER TABLE [BoardTasks] DROP COLUMN [BoardStatusId];
END

