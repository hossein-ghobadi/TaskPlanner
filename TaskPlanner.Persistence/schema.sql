CREATE TABLE [OutfitStyles] (
    [Id] int NOT NULL IDENTITY,
    [Title] nvarchar(max) NOT NULL,
    [ImageUrl] nvarchar(max) NOT NULL,
    [ContentType] nvarchar(max) NOT NULL,
    [SizeBytes] bigint NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_OutfitStyles] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [PersonalNotes] (
    [Id] int NOT NULL IDENTITY,
    [Title] nvarchar(max) NOT NULL,
    [Content] nvarchar(max) NULL,
    [UserId] nvarchar(max) NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    [Color] nvarchar(max) NULL,
    [IsPinned] bit NOT NULL,
    CONSTRAINT [PK_PersonalNotes] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [Projects] (
    [Id] int NOT NULL IDENTITY,
    [Name] nvarchar(200) NOT NULL,
    [Description] nvarchar(max) NULL,
    [IssueKeyPrefix] nvarchar(10) NOT NULL,
    [LastIssueNumber] int NOT NULL,
    [CreatorUserId] nvarchar(450) NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_Projects] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [TaskCategories] (
    [Id] int NOT NULL IDENTITY,
    [Name] nvarchar(max) NOT NULL,
    CONSTRAINT [PK_TaskCategories] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [User] (
    [Id] nvarchar(450) NOT NULL,
    [FullName] nvarchar(max) NOT NULL,
    [Gender] nvarchar(max) NULL,
    [Country] int NULL,
    [Province] int NULL,
    [City] int NULL,
    [Age] datetime2 NULL,
    [Phone] nvarchar(max) NULL,
    [Address] nvarchar(max) NULL,
    [IsRemove] bit NOT NULL,
    [IsActive] bit NOT NULL,
    [Latitude] float NULL,
    [Longitude] float NULL,
    [Documents] nvarchar(max) NULL,
    [InsertTime] datetime2 NOT NULL,
    [IsVarify] bit NOT NULL,
    [IsCentralOffice] bit NOT NULL,
    [UserName] nvarchar(max) NULL,
    [NormalizedUserName] nvarchar(max) NULL,
    [Email] nvarchar(max) NULL,
    [NormalizedEmail] nvarchar(max) NULL,
    [EmailConfirmed] bit NOT NULL,
    [PasswordHash] nvarchar(max) NULL,
    [SecurityStamp] nvarchar(max) NULL,
    [ConcurrencyStamp] nvarchar(max) NULL,
    [PhoneNumber] nvarchar(max) NULL,
    [PhoneNumberConfirmed] bit NOT NULL,
    [TwoFactorEnabled] bit NOT NULL,
    [LockoutEnd] datetimeoffset NULL,
    [LockoutEnabled] bit NOT NULL,
    [AccessFailedCount] int NOT NULL,
    CONSTRAINT [PK_User] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [UserPhotos] (
    [Id] int NOT NULL IDENTITY,
    [UserId] nvarchar(max) NOT NULL,
    [FilePath] nvarchar(max) NOT NULL,
    [ContentType] nvarchar(max) NOT NULL,
    [SizeBytes] bigint NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_UserPhotos] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [FavoriteOutfitStyles] (
    [Id] int NOT NULL IDENTITY,
    [UserId] nvarchar(450) NOT NULL,
    [OutfitStyleId] int NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_FavoriteOutfitStyles] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_FavoriteOutfitStyles_OutfitStyles_OutfitStyleId] FOREIGN KEY ([OutfitStyleId]) REFERENCES [OutfitStyles] ([Id]) ON DELETE CASCADE
);
GO


CREATE TABLE [PersonalNoteAttachments] (
    [Id] int NOT NULL IDENTITY,
    [PersonalNoteId] int NOT NULL,
    [FileName] nvarchar(max) NOT NULL,
    [FilePath] nvarchar(max) NOT NULL,
    [FileType] nvarchar(max) NOT NULL,
    [FileSize] bigint NOT NULL,
    [MimeType] nvarchar(max) NULL,
    [UploadedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_PersonalNoteAttachments] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_PersonalNoteAttachments_PersonalNotes_PersonalNoteId] FOREIGN KEY ([PersonalNoteId]) REFERENCES [PersonalNotes] ([Id]) ON DELETE CASCADE
);
GO


CREATE TABLE [ProjectInvitations] (
    [Id] int NOT NULL IDENTITY,
    [ProjectId] int NULL,
    [InviterId] nvarchar(max) NOT NULL,
    [InviteeId] nvarchar(450) NOT NULL,
    [InviteePhone] nvarchar(max) NOT NULL,
    [Status] int NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [RespondedAt] datetime2 NULL,
    CONSTRAINT [PK_ProjectInvitations] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_ProjectInvitations_Projects_ProjectId] FOREIGN KEY ([ProjectId]) REFERENCES [Projects] ([Id]) ON DELETE SET NULL
);
GO


CREATE TABLE [ProjectMembers] (
    [Id] int NOT NULL IDENTITY,
    [ProjectId] int NULL,
    [UserId] nvarchar(max) NOT NULL,
    CONSTRAINT [PK_ProjectMembers] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_ProjectMembers_Projects_ProjectId] FOREIGN KEY ([ProjectId]) REFERENCES [Projects] ([Id]) ON DELETE SET NULL
);
GO


CREATE TABLE [ProjectNotes] (
    [Id] int NOT NULL IDENTITY,
    [Title] nvarchar(max) NOT NULL,
    [Content] nvarchar(max) NULL,
    [ProjectId] int NOT NULL,
    [CreatorUserId] nvarchar(max) NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    CONSTRAINT [PK_ProjectNotes] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_ProjectNotes_Projects_ProjectId] FOREIGN KEY ([ProjectId]) REFERENCES [Projects] ([Id]) ON DELETE CASCADE
);
GO


CREATE TABLE [Sprints] (
    [Id] int NOT NULL IDENTITY,
    [Name] nvarchar(200) NOT NULL,
    [Description] nvarchar(1000) NULL,
    [Goal] nvarchar(500) NULL,
    [StartDate] datetime2 NOT NULL,
    [EndDate] datetime2 NOT NULL,
    [IsActive] bit NOT NULL,
    [IsCompleted] bit NOT NULL,
    [Status] int NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NOT NULL,
    [ProjectId] int NOT NULL,
    [CreatorUserId] nvarchar(max) NOT NULL,
    CONSTRAINT [PK_Sprints] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Sprints_Projects_ProjectId] FOREIGN KEY ([ProjectId]) REFERENCES [Projects] ([Id]) ON DELETE NO ACTION
);
GO


CREATE TABLE [WorkflowStatuses] (
    [Id] int NOT NULL IDENTITY,
    [Name] nvarchar(100) NOT NULL,
    [Description] nvarchar(max) NULL,
    [Color] nvarchar(20) NULL,
    [Order] int NOT NULL,
    [ProjectId] int NOT NULL,
    [Type] nvarchar(50) NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_WorkflowStatuses] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_WorkflowStatuses_Projects_ProjectId] FOREIGN KEY ([ProjectId]) REFERENCES [Projects] ([Id])
);
GO


CREATE TABLE [TryOnHistories] (
    [Id] int NOT NULL IDENTITY,
    [UserId] nvarchar(450) NOT NULL,
    [OutfitStyleId] int NULL,
    [UserPhotoId] int NULL,
    [OrderId] nvarchar(max) NOT NULL,
    [Status] nvarchar(max) NOT NULL,
    [ResultUrl] nvarchar(max) NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_TryOnHistories] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_TryOnHistories_OutfitStyles_OutfitStyleId] FOREIGN KEY ([OutfitStyleId]) REFERENCES [OutfitStyles] ([Id]) ON DELETE SET NULL,
    CONSTRAINT [FK_TryOnHistories_UserPhotos_UserPhotoId] FOREIGN KEY ([UserPhotoId]) REFERENCES [UserPhotos] ([Id]) ON DELETE SET NULL
);
GO


CREATE TABLE [UserStyles] (
    [Id] int NOT NULL IDENTITY,
    [UserId] nvarchar(max) NOT NULL,
    [Name] nvarchar(max) NOT NULL,
    [UserPhotoId] int NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_UserStyles] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_UserStyles_UserPhotos_UserPhotoId] FOREIGN KEY ([UserPhotoId]) REFERENCES [UserPhotos] ([Id]) ON DELETE NO ACTION
);
GO


CREATE TABLE [ProjectNoteAttachments] (
    [Id] int NOT NULL IDENTITY,
    [ProjectNoteId] int NOT NULL,
    [FileName] nvarchar(max) NOT NULL,
    [FilePath] nvarchar(max) NOT NULL,
    [FileType] nvarchar(max) NOT NULL,
    [FileSize] bigint NOT NULL,
    [MimeType] nvarchar(max) NULL,
    [UploadedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_ProjectNoteAttachments] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_ProjectNoteAttachments_ProjectNotes_ProjectNoteId] FOREIGN KEY ([ProjectNoteId]) REFERENCES [ProjectNotes] ([Id]) ON DELETE CASCADE
);
GO


CREATE TABLE [TaskItems] (
    [Id] int NOT NULL IDENTITY,
    [IssueKey] nvarchar(50) NULL,
    [Title] nvarchar(500) NOT NULL,
    [Description] nvarchar(max) NULL,
    [IssueType] int NOT NULL DEFAULT 3,
    [StartDate] datetime2 NOT NULL,
    [DueDate] datetime2 NULL,
    [IsCompleted] bit NOT NULL,
    [Priority] int NOT NULL,
    [StoryPoints] int NULL,
    [OriginalEstimateHours] decimal(18,2) NULL,
    [TimeSpentHours] decimal(18,2) NULL,
    [RemainingTimeHours] decimal(18,2) NULL,
    [CategoryId] int NULL,
    [AssignedUserId] nvarchar(450) NULL,
    [ProjectId] int NOT NULL,
    [SprintId] int NULL,
    [WorkflowStatusId] int NULL,
    [ParentTaskId] int NULL,
    [CreatedByUserId] nvarchar(max) NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NOT NULL,
    [TaskCategoryId] int NULL,
    CONSTRAINT [PK_TaskItems] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_TaskItems_Projects_ProjectId] FOREIGN KEY ([ProjectId]) REFERENCES [Projects] ([Id]),
    CONSTRAINT [FK_TaskItems_Sprints_SprintId] FOREIGN KEY ([SprintId]) REFERENCES [Sprints] ([Id]) ON DELETE SET NULL,
    CONSTRAINT [FK_TaskItems_TaskCategories_CategoryId] FOREIGN KEY ([CategoryId]) REFERENCES [TaskCategories] ([Id]) ON DELETE SET NULL,
    CONSTRAINT [FK_TaskItems_TaskCategories_TaskCategoryId] FOREIGN KEY ([TaskCategoryId]) REFERENCES [TaskCategories] ([Id]),
    CONSTRAINT [FK_TaskItems_TaskItems_ParentTaskId] FOREIGN KEY ([ParentTaskId]) REFERENCES [TaskItems] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_TaskItems_User_AssignedUserId] FOREIGN KEY ([AssignedUserId]) REFERENCES [User] ([Id]),
    CONSTRAINT [FK_TaskItems_WorkflowStatuses_WorkflowStatusId] FOREIGN KEY ([WorkflowStatusId]) REFERENCES [WorkflowStatuses] ([Id]) ON DELETE SET NULL
);
GO


CREATE TABLE [UserStyleItems] (
    [Id] int NOT NULL IDENTITY,
    [UserStyleId] int NOT NULL,
    [OutfitStyleId] int NOT NULL,
    CONSTRAINT [PK_UserStyleItems] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_UserStyleItems_OutfitStyles_OutfitStyleId] FOREIGN KEY ([OutfitStyleId]) REFERENCES [OutfitStyles] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_UserStyleItems_UserStyles_UserStyleId] FOREIGN KEY ([UserStyleId]) REFERENCES [UserStyles] ([Id]) ON DELETE CASCADE
);
GO


CREATE TABLE [SprintTasks] (
    [Id] int NOT NULL IDENTITY,
    [SprintId] int NOT NULL,
    [TaskId] int NOT NULL,
    [AddedAt] datetime2 NOT NULL,
    [AddedByUserId] nvarchar(max) NOT NULL,
    [Status] int NOT NULL,
    [SprintPriority] int NOT NULL,
    [SprintNotes] nvarchar(1000) NULL,
    [CompletedAt] datetime2 NULL,
    CONSTRAINT [PK_SprintTasks] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_SprintTasks_Sprints_SprintId] FOREIGN KEY ([SprintId]) REFERENCES [Sprints] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_SprintTasks_TaskItems_TaskId] FOREIGN KEY ([TaskId]) REFERENCES [TaskItems] ([Id]) ON DELETE NO ACTION
);
GO


CREATE TABLE [TaskComments] (
    [Id] int NOT NULL IDENTITY,
    [Message] nvarchar(max) NULL,
    [TaskId] int NOT NULL,
    [UserId] nvarchar(max) NOT NULL,
    [UserName] nvarchar(max) NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    [IsEdited] bit NOT NULL,
    [IsDeleted] bit NOT NULL,
    CONSTRAINT [PK_TaskComments] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_TaskComments_TaskItems_TaskId] FOREIGN KEY ([TaskId]) REFERENCES [TaskItems] ([Id]) ON DELETE CASCADE
);
GO


CREATE TABLE [TaskCommentAttachments] (
    [Id] int NOT NULL IDENTITY,
    [TaskCommentId] int NOT NULL,
    [FileName] nvarchar(max) NOT NULL,
    [FilePath] nvarchar(max) NOT NULL,
    [FileType] nvarchar(max) NOT NULL,
    [FileSize] bigint NOT NULL,
    [MimeType] nvarchar(max) NULL,
    [UploadedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_TaskCommentAttachments] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_TaskCommentAttachments_TaskComments_TaskCommentId] FOREIGN KEY ([TaskCommentId]) REFERENCES [TaskComments] ([Id]) ON DELETE CASCADE
);
GO


CREATE INDEX [IX_FavoriteOutfitStyles_OutfitStyleId] ON [FavoriteOutfitStyles] ([OutfitStyleId]);
GO


CREATE UNIQUE INDEX [IX_FavoriteOutfitStyles_UserId_OutfitStyleId] ON [FavoriteOutfitStyles] ([UserId], [OutfitStyleId]);
GO


CREATE INDEX [IX_PersonalNoteAttachments_PersonalNoteId] ON [PersonalNoteAttachments] ([PersonalNoteId]);
GO


CREATE INDEX [IX_ProjectInvitations_ProjectId_InviteeId_Status] ON [ProjectInvitations] ([ProjectId], [InviteeId], [Status]);
GO


CREATE INDEX [IX_ProjectMembers_ProjectId] ON [ProjectMembers] ([ProjectId]);
GO


CREATE INDEX [IX_ProjectNoteAttachments_ProjectNoteId] ON [ProjectNoteAttachments] ([ProjectNoteId]);
GO


CREATE INDEX [IX_ProjectNotes_ProjectId] ON [ProjectNotes] ([ProjectId]);
GO


CREATE INDEX [IX_Projects_IssueKeyPrefix] ON [Projects] ([IssueKeyPrefix]);
GO


CREATE INDEX [IX_Sprints_ProjectId] ON [Sprints] ([ProjectId]);
GO


CREATE UNIQUE INDEX [IX_SprintTasks_SprintId_TaskId] ON [SprintTasks] ([SprintId], [TaskId]);
GO


CREATE INDEX [IX_SprintTasks_TaskId] ON [SprintTasks] ([TaskId]);
GO


CREATE INDEX [IX_TaskCommentAttachments_TaskCommentId] ON [TaskCommentAttachments] ([TaskCommentId]);
GO


CREATE INDEX [IX_TaskComments_TaskId] ON [TaskComments] ([TaskId]);
GO


CREATE INDEX [IX_TaskItems_AssignedUserId] ON [TaskItems] ([AssignedUserId]);
GO


CREATE INDEX [IX_TaskItems_CategoryId] ON [TaskItems] ([CategoryId]);
GO


CREATE UNIQUE INDEX [IX_TaskItems_IssueKey] ON [TaskItems] ([IssueKey]) WHERE [IssueKey] IS NOT NULL;
GO


CREATE INDEX [IX_TaskItems_ParentTaskId] ON [TaskItems] ([ParentTaskId]);
GO


CREATE INDEX [IX_TaskItems_ProjectId_IssueType] ON [TaskItems] ([ProjectId], [IssueType]);
GO


CREATE INDEX [IX_TaskItems_SprintId] ON [TaskItems] ([SprintId]);
GO


CREATE INDEX [IX_TaskItems_TaskCategoryId] ON [TaskItems] ([TaskCategoryId]);
GO


CREATE INDEX [IX_TaskItems_WorkflowStatusId] ON [TaskItems] ([WorkflowStatusId]);
GO


CREATE INDEX [IX_TryOnHistories_OutfitStyleId] ON [TryOnHistories] ([OutfitStyleId]);
GO


CREATE INDEX [IX_TryOnHistories_UserId_UserPhotoId_OutfitStyleId_CreatedAt] ON [TryOnHistories] ([UserId], [UserPhotoId], [OutfitStyleId], [CreatedAt]);
GO


CREATE INDEX [IX_TryOnHistories_UserPhotoId] ON [TryOnHistories] ([UserPhotoId]);
GO


CREATE INDEX [IX_UserStyleItems_OutfitStyleId] ON [UserStyleItems] ([OutfitStyleId]);
GO


CREATE INDEX [IX_UserStyleItems_UserStyleId] ON [UserStyleItems] ([UserStyleId]);
GO


CREATE INDEX [IX_UserStyles_UserPhotoId] ON [UserStyles] ([UserPhotoId]);
GO


CREATE INDEX [IX_WorkflowStatuses_ProjectId] ON [WorkflowStatuses] ([ProjectId]);
GO


