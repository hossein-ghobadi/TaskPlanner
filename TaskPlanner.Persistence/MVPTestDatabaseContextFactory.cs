using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using TaskPlanner.Persistence.Contexts;

namespace TaskPlanner.Persistence;

/// <summary>
/// اتصال design-time برای دستورات dotnet ef (مigrations).
/// قبل از اجرا متغیر محیطی CONNECTION_MVPTest را تنظیم کنید یا در launchSettings / سیستم ست کنید.
/// </summary>
public sealed class MVPTestDatabaseContextFactory : IDesignTimeDbContextFactory<MVPTestDatabaseContext>
{
    public MVPTestDatabaseContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("CONNECTION_MVPTest");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "برای ساخت migration، متغیر محیطی CONNECTION_MVPTest را روی رشته اتصال SQL Server تنظیم کنید.");
        }

        var optionsBuilder = new DbContextOptionsBuilder<MVPTestDatabaseContext>();
        optionsBuilder.UseSqlServer(connectionString, sql =>
            sql.MigrationsAssembly(typeof(MVPTestDatabaseContext).Assembly.GetName().Name!));

        return new MVPTestDatabaseContext(optionsBuilder.Options);
    }
}
