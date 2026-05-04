

using Microsoft.EntityFrameworkCore;



using DotNetEnv;
using TaskPlanner.Persistence.Contexts;
using TaskPlanner.Infrastructure.IdentityConfigs;
using TaskPlanner.Application.Interfaces.Contexts;
using TaskPlanner.Infrastructure.DependencyInjections;

using System;

using Microsoft.AspNetCore.Identity;
using TaskPlanner.Domain.Entities.Users;
using Microsoft.Extensions.Options;
using TaskPlanner.Application.Services;
using Microsoft.Extensions.Logging;
using Endpoint.Site.Hubs;


Env.Load();

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddEnvironmentVariables(); // (اختیاری ولی صریح)

// اجازه آپلود فایل‌های بزرگ (مثلاً zip طراحی فیگما) — پیش‌فرض Kestrel ~30MB است و باعث خطای 400 می‌شود
builder.WebHost.ConfigureKestrel(opt => opt.Limits.MaxRequestBodySize = 250_000_000); // 250 MB

builder.Services.AddHttpContextAccessor();

// Add services to the container.
builder.Services.AddControllers();

builder.Services.AddControllersWithViews();
var configuration = builder.Configuration;

var connection3 = Environment.GetEnvironmentVariable("CONNECTION_MVPTest");

// Register merged context (used by both Identity and business logic) - MUST be before AddIdentityService
builder.Services.AddDbContext<MVPTestDatabaseContext>(options =>
{
    options.UseSqlServer(connection3);//, b => b.MigrationsAssembly("EndPoint.Site")
});

builder.Services.AddScoped<IMVPTestDatabaseContext, MVPTestDatabaseContext>();

// سرویس آپلود فایل
builder.Services.AddScoped<TaskPlanner.Application.Services.FileUpload.IFileUploadService, TaskPlanner.Application.Services.FileUpload.FileUploadService>();

// Add Identity services - MUST be after AddDbContext
builder.Services.AddIdentityService(builder.Configuration);

builder.Services.AddHttpClient("PriceApi", c =>
{
    var baseUrl = builder.Configuration["ApiBaseUrl"] ?? "https://localhost:7000/";
    c.BaseAddress = new Uri(baseUrl);
    c.Timeout = TimeSpan.FromSeconds(30);
});





builder.Services.AddInfrastructureServices(builder.Configuration);

builder.Services.AddHttpClient();  // برای IHttpClientFactory
builder.Services.AddSignalR();


var app = builder.Build();

// Ensure ParentTaskId column exists in BoardTasks table
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<MVPTestDatabaseContext>();
    try
    {
        // Check if ParentTaskId column exists
        var sql = @"
            IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[BoardTasks]') AND name = 'ParentTaskId')
            BEGIN
                ALTER TABLE [BoardTasks] ADD [ParentTaskId] int NULL;
                CREATE INDEX [IX_BoardTasks_ParentTaskId] ON [BoardTasks] ([ParentTaskId]);
                ALTER TABLE [BoardTasks] ADD CONSTRAINT [FK_BoardTasks_BoardTasks_ParentTaskId] 
                FOREIGN KEY ([ParentTaskId]) REFERENCES [BoardTasks] ([Id]) ON DELETE NO ACTION;
            END";
        await context.Database.ExecuteSqlRawAsync(sql);

        var chatTablesSql = @"
            IF OBJECT_ID(N'[dbo].[ProjectChatGroups]', N'U') IS NULL
            BEGIN
                CREATE TABLE [dbo].[ProjectChatGroups](
                    [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                    [ProjectId] INT NOT NULL,
                    [Name] NVARCHAR(150) NOT NULL,
                    [CreatedByUserId] NVARCHAR(450) NOT NULL,
                    [CreatedAt] DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
                    [UpdatedAt] DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
                    [IsArchived] BIT NOT NULL DEFAULT 0,
                    CONSTRAINT [FK_ProjectChatGroups_Projects_ProjectId] FOREIGN KEY ([ProjectId]) REFERENCES [Projects]([Id]) ON DELETE CASCADE
                );
                CREATE INDEX [IX_ProjectChatGroups_ProjectId_Name] ON [dbo].[ProjectChatGroups]([ProjectId], [Name]);
            END;

            IF OBJECT_ID(N'[dbo].[ProjectChatGroupMembers]', N'U') IS NULL
            BEGIN
                CREATE TABLE [dbo].[ProjectChatGroupMembers](
                    [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                    [ProjectChatGroupId] INT NOT NULL,
                    [UserId] NVARCHAR(450) NOT NULL,
                    [AddedByUserId] NVARCHAR(450) NULL,
                    [AddedAt] DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
                    CONSTRAINT [FK_ProjectChatGroupMembers_ProjectChatGroups_ProjectChatGroupId] FOREIGN KEY ([ProjectChatGroupId]) REFERENCES [ProjectChatGroups]([Id]) ON DELETE CASCADE
                );
                CREATE UNIQUE INDEX [IX_ProjectChatGroupMembers_ProjectChatGroupId_UserId] ON [dbo].[ProjectChatGroupMembers]([ProjectChatGroupId], [UserId]);
            END;

            IF OBJECT_ID(N'[dbo].[ProjectChatMessages]', N'U') IS NULL
            BEGIN
                CREATE TABLE [dbo].[ProjectChatMessages](
                    [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                    [ProjectChatGroupId] INT NOT NULL,
                    [UserId] NVARCHAR(450) NOT NULL,
                    [UserName] NVARCHAR(200) NOT NULL,
                    [Message] NVARCHAR(4000) NOT NULL,
                    [ReplyToMessageId] INT NULL,
                    [CreatedAt] DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
                    [IsDeleted] BIT NOT NULL DEFAULT 0,
                    CONSTRAINT [FK_ProjectChatMessages_ProjectChatGroups_ProjectChatGroupId] FOREIGN KEY ([ProjectChatGroupId]) REFERENCES [ProjectChatGroups]([Id]) ON DELETE CASCADE,
                    CONSTRAINT [FK_ProjectChatMessages_ProjectChatMessages_ReplyToMessageId] FOREIGN KEY ([ReplyToMessageId]) REFERENCES [ProjectChatMessages]([Id])
                );
                CREATE INDEX [IX_ProjectChatMessages_ProjectChatGroupId_CreatedAt] ON [dbo].[ProjectChatMessages]([ProjectChatGroupId], [CreatedAt]);
                CREATE INDEX [IX_ProjectChatMessages_ReplyToMessageId] ON [dbo].[ProjectChatMessages]([ReplyToMessageId]);
            END;

            IF COL_LENGTH('dbo.ProjectChatMessages', 'ReplyToMessageId') IS NULL
            BEGIN
                ALTER TABLE [dbo].[ProjectChatMessages] ADD [ReplyToMessageId] INT NULL;
            END;

            IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_ProjectChatMessages_ReplyToMessageId' AND object_id = OBJECT_ID(N'[dbo].[ProjectChatMessages]'))
            BEGIN
                CREATE INDEX [IX_ProjectChatMessages_ReplyToMessageId] ON [dbo].[ProjectChatMessages]([ReplyToMessageId]);
            END;

            IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_ProjectChatMessages_ProjectChatMessages_ReplyToMessageId')
            BEGIN
                ALTER TABLE [dbo].[ProjectChatMessages]
                ADD CONSTRAINT [FK_ProjectChatMessages_ProjectChatMessages_ReplyToMessageId]
                FOREIGN KEY ([ReplyToMessageId]) REFERENCES [dbo].[ProjectChatMessages]([Id]);
            END;

            IF OBJECT_ID(N'[dbo].[ProjectChatMessageAttachments]', N'U') IS NULL
            BEGIN
                CREATE TABLE [dbo].[ProjectChatMessageAttachments](
                    [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                    [ProjectChatMessageId] INT NOT NULL,
                    [FileName] NVARCHAR(500) NOT NULL,
                    [FilePath] NVARCHAR(1000) NOT NULL,
                    [FileType] NVARCHAR(50) NOT NULL,
                    [FileSize] BIGINT NOT NULL,
                    [MimeType] NVARCHAR(100) NULL,
                    [UploadedAt] DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
                    CONSTRAINT [FK_ProjectChatMessageAttachments_ProjectChatMessages_ProjectChatMessageId] FOREIGN KEY ([ProjectChatMessageId]) REFERENCES [ProjectChatMessages]([Id]) ON DELETE CASCADE
                );
            END;";
        await context.Database.ExecuteSqlRawAsync(chatTablesSql);

        var leadsTableSql = @"
            IF OBJECT_ID(N'[dbo].[Leads]', N'U') IS NULL
            BEGIN
                CREATE TABLE [dbo].[Leads](
                    [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                    [Title] NVARCHAR(200) NOT NULL,
                    [CompanyName] NVARCHAR(200) NULL,
                    [ContactName] NVARCHAR(200) NULL,
                    [Phone] NVARCHAR(50) NULL,
                    [Email] NVARCHAR(256) NULL,
                    [Notes] NVARCHAR(MAX) NULL,
                    [Source] NVARCHAR(100) NULL,
                    [Status] INT NOT NULL CONSTRAINT [DF_Leads_Status] DEFAULT 0,
                    [OwnerUserId] NVARCHAR(450) NOT NULL,
                    [CreatedAt] DATETIME2 NOT NULL CONSTRAINT [DF_Leads_CreatedAt] DEFAULT SYSUTCDATETIME(),
                    [UpdatedAt] DATETIME2 NOT NULL CONSTRAINT [DF_Leads_UpdatedAt] DEFAULT SYSUTCDATETIME(),
                    [ConvertedProjectId] INT NULL,
                    [ConvertedAt] DATETIME2 NULL,
                    CONSTRAINT [FK_Leads_Projects_ConvertedProjectId] FOREIGN KEY ([ConvertedProjectId]) REFERENCES [Projects]([Id]) ON DELETE SET NULL
                );
                CREATE INDEX [IX_Leads_OwnerUserId] ON [dbo].[Leads]([OwnerUserId]);
                CREATE INDEX [IX_Leads_Status] ON [dbo].[Leads]([Status]);
            END;";
        await context.Database.ExecuteSqlRawAsync(leadsTableSql);
    }
    catch (Exception ex)
    {
        // Log error but don't stop application startup
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogWarning(ex, "Failed to ensure ParentTaskId column exists. Please run the SQL script manually.");
    }
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseCors("MyCors");

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();
app.UseAuthentication();

app.UseAuthorization();

app.UseEndpoints(endpoints =>
{
    // Map attribute-routed controllers
    endpoints.MapControllers();

    // Map conventional routes
    endpoints.MapControllerRoute(
        name: "areas",
        pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");
    endpoints.MapControllerRoute(
        name: "default",
        pattern: "{controller=Projects}/{action=Index}/{id?}");
    endpoints.MapHub<ProjectChatHub>("/chathub");
});

app.Run();
