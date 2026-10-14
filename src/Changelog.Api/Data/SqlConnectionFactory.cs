using System.Data.Common;
using Microsoft.Data.SqlClient;

namespace Changelog.Api.Data;

public sealed class SqlConnectionFactory(IConfiguration configuration)
{
    private readonly string _connectionString = configuration.GetConnectionString("Changelog")
        ?? throw new InvalidOperationException("Connection string 'Changelog' is not configured.");

    public string ConnectionString => _connectionString;

    public async Task<DbConnection> OpenAsync(CancellationToken ct = default)
    {
        var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(ct);
        return connection;
    }
}
