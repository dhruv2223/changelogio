using System.Net.Http.Headers;
using System.Net.Http.Json;
using Changelog.Core;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.SqlClient;

namespace Changelog.Api.Tests;

/// <summary>
/// Hosts the API in-memory. When CHANGELOG_TEST_SQL is set (a SQL Server connection string without a database),
/// a fresh, uniquely named database is created for the run and dropped afterwards.
/// </summary>
public sealed class ApiFactory : WebApplicationFactory<Program>
{
    public const string ConnectionVariable = "CHANGELOG_TEST_SQL";

    private readonly string? _connectionString;
    private int _dropped;

    public ApiFactory()
    {
        var server = Environment.GetEnvironmentVariable(ConnectionVariable);
        if (!string.IsNullOrWhiteSpace(server))
        {
            _connectionString = new SqlConnectionStringBuilder(server)
            {
                InitialCatalog = $"ChangelogTests_{Guid.NewGuid():N}",
            }.ConnectionString;
        }
    }

    protected override void ConfigureWebHost(Microsoft.AspNetCore.Hosting.IWebHostBuilder builder)
    {
        // Without a database the tests only exercise auth and validation, which reject before any query runs.
        builder.UseSetting("ConnectionStrings:Changelog",
            _connectionString ?? "Server=unused.invalid;Database=Unused;Connect Timeout=1");
        builder.UseSetting("Database:AutoMigrate", (_connectionString is not null).ToString());
    }

    public HttpClient CreateClient(string token)
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    /// <summary>Registers a fresh user and returns a client authenticated as them.</summary>
    public async Task<HttpClient> CreateUserClientAsync()
    {
        var response = await CreateClient().PostAsJsonAsync("/user",
            new AuthRequest { Username = $"user-{Guid.NewGuid():N}", Password = "correct-horse" });
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<TokenResponse>();
        return CreateClient(body!.Token);
    }

    public override async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();
        if (_connectionString is null || Interlocked.Exchange(ref _dropped, 1) == 1) return;

        var builder = new SqlConnectionStringBuilder(_connectionString);
        var database = builder.InitialCatalog;
        builder.InitialCatalog = "master";
        SqlConnection.ClearAllPools();
        await using var master = new SqlConnection(builder.ConnectionString);
        await master.OpenAsync();
        await using var drop = master.CreateCommand();
        drop.CommandText = $"""
            IF DB_ID(N'{database}') IS NOT NULL
            BEGIN
                ALTER DATABASE [{database}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
                DROP DATABASE [{database}];
            END
            """;
        await drop.ExecuteNonQueryAsync();
    }
}

/// <summary>A fact that only runs when a SQL Server is available via CHANGELOG_TEST_SQL.</summary>
public sealed class SqlServerFactAttribute : FactAttribute
{
    public SqlServerFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(ApiFactory.ConnectionVariable)))
            Skip = $"Set {ApiFactory.ConnectionVariable} to a SQL Server connection string to run database tests.";
    }
}
