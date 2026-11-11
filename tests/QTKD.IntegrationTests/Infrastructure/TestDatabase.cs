using Microsoft.Extensions.Configuration;
using Npgsql;

namespace QTKD.IntegrationTests.Infrastructure;

public static class TestDatabase
{
    private const string ApiUserSecretsId = "qtkd-api-6f3c1b8e-2d4a-4e7b-9c15-a8d3f0b7e2c9";
    private const string DefaultDatabaseName = "qtkd_test";

    public static string ResolveConnectionString()
    {
        var configuration = new ConfigurationBuilder()
            .AddUserSecrets(ApiUserSecretsId)
            .AddEnvironmentVariables()
            .Build();

        var testConnection = configuration.GetConnectionString("QtkdTestDb");
        NpgsqlConnectionStringBuilder builder;
        if (!string.IsNullOrWhiteSpace(testConnection))
        {
            builder = new NpgsqlConnectionStringBuilder(testConnection);
        }
        else
        {
            var apiConnection = configuration.GetConnectionString("QtkdDb")
                ?? throw new InvalidOperationException(
                    "Thiếu chuỗi kết nối. Đặt ConnectionStrings:QtkdDb cho API (README mục 4) hoặc ConnectionStrings:QtkdTestDb.");
            builder = new NpgsqlConnectionStringBuilder(apiConnection) { Database = DefaultDatabaseName };
        }

        if (builder.Database is null || !builder.Database.EndsWith("_test", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException(
                $"Integration test chỉ chạy trên database có tên kết thúc bằng _test; hiện là '{builder.Database}'.");

        return builder.ConnectionString;
    }
}
