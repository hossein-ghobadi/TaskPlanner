-- Migration: AddUserProjectFolder
-- این SQL را مستقیماً در SQL Server Management Studio اجرا کنید

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
GO

CREATE INDEX [IX_UserProjectFolders_FolderId] ON [UserProjectFolders] ([FolderId]);
GO

CREATE INDEX [IX_UserProjectFolders_ProjectId] ON [UserProjectFolders] ([ProjectId]);
GO

CREATE UNIQUE INDEX [IX_UserProjectFolders_UserId_ProjectId] ON [UserProjectFolders] ([UserId], [ProjectId]);
GO
















