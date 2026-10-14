using System.Text.RegularExpressions;
using Dapper;
using Microsoft.Data.SqlClient;

namespace Changelog.Api.Data;

/// <summary>
/// Creates the database (if missing) and applies the embedded database/schema.sql.
/// Enabled with Database:AutoMigrate = true; the script itself is idempotent.
/// </summary>
public static partial class DatabaseInitializer
{
    public static async Task InitializeAsync(string connectionString, ILogger logger, CancellationToken ct = default)
    {
        var builder = new SqlConnectionStringBuilder(connectionString);
        var databaseName = builder.InitialCatalog;
        if (string.IsNullOrWhiteSpace(databaseName))
            throw new InvalidOperationException("The Changelog connection string must specify a Database / Initial Catalog.");

        builder.InitialCatalog = "master";
        await using (var master = new SqlConnection(builder.ConnectionString))
        {
            await master.OpenAsync(ct);
            // EXEC() only accepts variables and literals, so build the statement first.
            await master.ExecuteAsync("""
                IF DB_ID(@name) IS NULL
                BEGIN
                    DECLARE @sql NVARCHAR(400) = N'CREATE DATABASE ' + QUOTENAME(@name);
                    EXEC (@sql);
                END
                """,
                new { name = databaseName });
        }

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(ct);
        foreach (var batch in GoSeparator().Split(ReadSchema()).Where(b => !string.IsNullOrWhiteSpace(b)))
            await connection.ExecuteAsync(batch);

        logger.LogInformation("Database {Database} is up to date", databaseName);
    }

    private static string ReadSchema()
    {
        using var stream = typeof(DatabaseInitializer).Assembly.GetManifestResourceStream("Changelog.Api.schema.sql")
            ?? throw new InvalidOperationException("Embedded schema.sql not found.");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    [GeneratedRegex(@"^\s*GO\s*$", RegexOptions.Multiline | RegexOptions.IgnoreCase)]
    private static partial Regex GoSeparator();
}
