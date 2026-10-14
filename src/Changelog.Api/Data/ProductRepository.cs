using Changelog.Core;
using Dapper;

namespace Changelog.Api.Data;

/// <summary>Every query is scoped to the owning user, so one user can never read or change another's products.</summary>
public sealed class ProductRepository(SqlConnectionFactory db)
{
    private const string Columns = "Id, CreatedAt, Name, BelongsToId";

    public async Task<IReadOnlyList<Product>> ListAsync(Guid userId, CancellationToken ct = default)
    {
        await using var connection = await db.OpenAsync(ct);
        var rows = await connection.QueryAsync<Product>(new CommandDefinition(
            $"SELECT {Columns} FROM dbo.Products WHERE BelongsToId = @userId ORDER BY CreatedAt;",
            new { userId }, cancellationToken: ct));
        return rows.AsList();
    }

    public async Task<Product?> GetAsync(Guid id, Guid userId, CancellationToken ct = default)
    {
        await using var connection = await db.OpenAsync(ct);
        return await connection.QuerySingleOrDefaultAsync<Product>(new CommandDefinition(
            $"SELECT {Columns} FROM dbo.Products WHERE Id = @id AND BelongsToId = @userId;",
            new { id, userId }, cancellationToken: ct));
    }

    public async Task<Product> CreateAsync(Guid userId, string name, CancellationToken ct = default)
    {
        await using var connection = await db.OpenAsync(ct);
        return await connection.QuerySingleAsync<Product>(new CommandDefinition("""
            INSERT INTO dbo.Products (Name, BelongsToId)
            OUTPUT INSERTED.Id, INSERTED.CreatedAt, INSERTED.Name, INSERTED.BelongsToId
            VALUES (@name, @userId);
            """, new { name, userId }, cancellationToken: ct));
    }

    public async Task<Product?> RenameAsync(Guid id, Guid userId, string name, CancellationToken ct = default)
    {
        await using var connection = await db.OpenAsync(ct);
        return await connection.QuerySingleOrDefaultAsync<Product>(new CommandDefinition("""
            UPDATE dbo.Products SET Name = @name
            OUTPUT INSERTED.Id, INSERTED.CreatedAt, INSERTED.Name, INSERTED.BelongsToId
            WHERE Id = @id AND BelongsToId = @userId;
            """, new { id, userId, name }, cancellationToken: ct));
    }

    /// <summary>Deletes the product; its updates and update points go with it (ON DELETE CASCADE).</summary>
    public async Task<Product?> DeleteAsync(Guid id, Guid userId, CancellationToken ct = default)
    {
        await using var connection = await db.OpenAsync(ct);
        return await connection.QuerySingleOrDefaultAsync<Product>(new CommandDefinition("""
            DELETE FROM dbo.Products
            OUTPUT DELETED.Id, DELETED.CreatedAt, DELETED.Name, DELETED.BelongsToId
            WHERE Id = @id AND BelongsToId = @userId;
            """, new { id, userId }, cancellationToken: ct));
    }
}
