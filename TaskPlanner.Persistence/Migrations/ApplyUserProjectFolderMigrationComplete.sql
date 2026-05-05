-- Migration: AddUserProjectFolder
-- Run this SQL script directly in SQL Server Management Studio
-- This will create the UserProjectFolders table and mark the migration as applied

-- Check if table already exists
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[UserProjectFolders]') AND type in (N'U'))
BEGIN
    CREATE TABLE [UserProjectFolders] (
        [Id] int NOT NULL IDENTITY(1,1),
        [UserId] nvarchar(450) NOT NULL,
        [ProjectId] int NOT NULL,
        [FolderId] int NULL,
        [Order] int NOT NULL DEFAULT 0,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_UserProjectFolders] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_UserProjectFolders_ProjectFolders_FolderId] FOREIGN KEY ([FolderId]) REFERENCES [ProjectFolders] ([Id]) ON DELETE SET NULL,
        CONSTRAINT [FK_UserProjectFolders_Projects_ProjectId] FOREIGN KEY ([ProjectId]) REFERENCES [Projects] ([Id]) ON DELETE CASCADE
    );
    
    CREATE INDEX [IX_UserProjectFolders_FolderId] ON [UserProjectFolders] ([FolderId]);
    CREATE INDEX [IX_UserProjectFolders_ProjectId] ON [UserProjectFolders] ([ProjectId]);
    CREATE UNIQUE INDEX [IX_UserProjectFolders_UserId_ProjectId] ON [UserProjectFolders] ([UserId], [ProjectId]);
    
    PRINT 'UserProjectFolders table created successfully!';
END
ELSE
BEGIN
    PRINT 'UserProjectFolders table already exists.';
END
GO

-- Mark the migration as applied in the history table
IF NOT EXISTS (SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = '20251206120000_AddUserProjectFolder')
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES ('20251206120000_AddUserProjectFolder', '8.0.4');
    PRINT 'Migration marked as applied in history table.';
END
ELSE
BEGIN
    PRINT 'Migration already marked as applied.';
END
GO

