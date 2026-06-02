

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
using Endpoint.Site.Hubs;
using Endpoint.Site.Services.Bale;
using Microsoft.AspNetCore.ResponseCompression;
using System.IO.Compression;
using Microsoft.Net.Http.Headers;


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
    options.UseSqlServer(connection3, sql =>
        sql.MigrationsAssembly(typeof(MVPTestDatabaseContext).Assembly.GetName().Name!));
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

builder.Services.AddScoped<TaskPlanner.Application.Services.Bale.IBaleChatNotifier, BaleSignalRChatNotifier>();

builder.Services.AddSignalR();

// ===== Response Compression (Brotli + Gzip) =====
// تقریباً ۷۰٪ حجم HTML/CSS/JS را کم می‌کند و روی شبکه‌های کند فرقش محسوس است
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
    options.Providers.Add<BrotliCompressionProvider>();
    options.Providers.Add<GzipCompressionProvider>();
    options.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(new[]
    {
        "application/javascript",
        "application/json",
        "image/svg+xml",
        "font/woff",
        "font/woff2",
        "application/font-woff",
        "application/font-woff2"
    });
});

builder.Services.Configure<BrotliCompressionProviderOptions>(o => o.Level = CompressionLevel.Optimal);
builder.Services.Configure<GzipCompressionProviderOptions>(o => o.Level = CompressionLevel.Optimal);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseCors("MyCors");

// Compression باید قبل از UseStaticFiles باشد تا فایل‌های استاتیک هم فشرده شوند
app.UseResponseCompression();

app.UseHttpsRedirection();

// ===== Static Files با Cache طولانی =====
// چون از asp-append-version="true" استفاده می‌کنیم، URL با تغییر فایل عوض می‌شود
// پس immutable و یک ساله امن است
app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = ctx =>
    {
        const int durationInSeconds = 60 * 60 * 24 * 365; // یک سال
        ctx.Context.Response.Headers[HeaderNames.CacheControl] =
            $"public, max-age={durationInSeconds}, immutable";
    }
});

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
