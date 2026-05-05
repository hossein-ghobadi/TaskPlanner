-- Add ParentTaskId column to BoardTasks table for subtasks support
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[BoardTasks]') AND name = 'ParentTaskId')
BEGIN
    ALTER TABLE [BoardTasks]
    ADD [ParentTaskId] int NULL;

    CREATE INDEX [IX_BoardTasks_ParentTaskId] ON [BoardTasks] ([ParentTaskId]);

    ALTER TABLE [BoardTasks]
    ADD CONSTRAINT [FK_BoardTasks_BoardTasks_ParentTaskId] 
    FOREIGN KEY ([ParentTaskId]) REFERENCES [BoardTasks] ([Id]) ON DELETE NO ACTION;

    PRINT 'ParentTaskId column added successfully to BoardTasks table.';
END
ELSE
BEGIN
    PRINT 'ParentTaskId column already exists in BoardTasks table.';
END
GO

