

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

var allowedOrigin1 = Environment.GetEnvironmentVariable("ALLOWED_ORIGIN1");
var allowedOrigin2 = Environment.GetEnvironmentVariable("ALLOWED_ORIGIN2");
var allowedOrigin3 = Environment.GetEnvironmentVariable("ALLOWED_ORIGIN3");
var allowedOrigin4 = Environment.GetEnvironmentVariable("ALLOWED_ORIGIN4");
var allowedOrigin5 = Environment.GetEnvironmentVariable("ALLOWED_ORIGIN5");
var allowedOrigin6 = Environment.GetEnvironmentVariable("ALLOWED_ORIGIN6");

builder.Services.AddCors(options =>
{
    options.AddPolicy("MyCors", builder =>
    {
        builder.WithOrigins(allowedOrigin1, allowedOrigin2, allowedOrigin3, allowedOrigin4, allowedOrigin5, allowedOrigin6)
               .AllowAnyHeader()
               .AllowAnyMethod()
               .AllowCredentials()
               .SetIsOriginAllowedToAllowWildcardSubdomains(); // Optional: Allow subdomains

    });
});

builder.Services.AddInfrastructureServices(builder.Configuration);

builder.Services.AddHttpClient();  // برای IHttpClientFactory


var app = builder.Build();
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
        pattern: "{controller=Home}/{action=Index}/{id?}");
});

app.Run();
