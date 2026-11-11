using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using QTKD.Api.Data;

namespace QTKD.IntegrationTests.Infrastructure;

public sealed class QtkdApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    public QtkdApiFactory()
    {
        ConnectionString = TestDatabase.ResolveConnectionString();
        Environment.SetEnvironmentVariable("ConnectionStrings__QtkdDb", ConnectionString);
    }

    public string ConnectionString { get; }

    protected override void ConfigureWebHost(IWebHostBuilder builder) => builder.UseEnvironment("Testing");

    public QtkdDbContext CreateDbContext()
        => new(new DbContextOptionsBuilder<QtkdDbContext>().UseNpgsql(ConnectionString).Options);

    public async Task InitializeAsync()
    {
        await using var db = CreateDbContext();
        await db.Database.MigrateAsync();
    }

    public async Task ResetDatabaseAsync()
    {
        await using var db = CreateDbContext();
        await db.Database.ExecuteSqlRawAsync(
            """TRUNCATE TABLE "ApprovalRequests", "Contracts", "Customers", "PriceItems", "PriceLists" RESTART IDENTITY CASCADE;""");
    }

    Task IAsyncLifetime.DisposeAsync() => Task.CompletedTask;
}
