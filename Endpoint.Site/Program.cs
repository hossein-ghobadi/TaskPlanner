

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


Env.Load();

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddEnvironmentVariables(); // (اختیاری ولی صریح)
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
});

app.Run();
