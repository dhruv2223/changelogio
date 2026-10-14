using Dapper;
using Microsoft.Data.SqlClient;

namespace Changelog.Api.Data;

public sealed class UserRecord
{
    public Guid Id { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public string Username { get; set; } = "";
    public string PasswordHash { get; set; } = "";
}

public sealed class DuplicateUsernameException(string username)
    : Exception($"Username '{username}' is already taken.");

public sealed class UserRepository(SqlConnectionFactory db)
{
    public async Task<UserRecord> CreateAsync(string username, string passwordHash, CancellationToken ct = default)
    {
        await using var connection = await db.OpenAsync(ct);
        try
        {
            return await connection.QuerySingleAsync<UserRecord>(new CommandDefinition("""
                INSERT INTO dbo.Users (Username, PasswordHash)
                OUTPUT INSERTED.Id, INSERTED.CreatedAt, INSERTED.Username, INSERTED.PasswordHash
                VALUES (@username, @passwordHash);
                """, new { username, passwordHash }, cancellationToken: ct));
        }
        catch (SqlException ex) when (ex.Number is 2627 or 2601) // unique constraint / unique index violation
        {
            throw new DuplicateUsernameException(username);
        }
    }

    public async Task<UserRecord?> FindByUsernameAsync(string username, CancellationToken ct = default)
    {
        await using var connection = await db.OpenAsync(ct);
        return await connection.QuerySingleOrDefaultAsync<UserRecord>(new CommandDefinition(
            "SELECT Id, CreatedAt, Username, PasswordHash FROM dbo.Users WHERE Username = @username;",
            new { username }, cancellationToken: ct));
    }
}
