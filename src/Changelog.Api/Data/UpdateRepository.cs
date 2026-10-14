using Changelog.Core;
using Dapper;

namespace Changelog.Api.Data;

/// <summary>Updates are owned through their product; every query joins to dbo.Products to enforce that.</summary>
public sealed class UpdateRepository(SqlConnectionFactory db)
{
    private const string Inserted = "INSERTED.Id, INSERTED.CreatedAt, INSERTED.Title, INSERTED.Body, INSERTED.Status, INSERTED.Version, INSERTED.Asset, INSERTED.ProductId";
    private const string Deleted = "DELETED.Id, DELETED.CreatedAt, DELETED.Title, DELETED.Body, DELETED.Status, DELETED.Version, DELETED.Asset, DELETED.ProductId";
    private const string Selected = "u.Id, u.CreatedAt, u.Title, u.Body, u.Status, u.Version, u.Asset, u.ProductId";

    public async Task<IReadOnlyList<Update>> ListAsync(Guid userId, Guid? productId, CancellationToken ct = default)
    {
        await using var connection = await db.OpenAsync(ct);
        var rows = await connection.QueryAsync<Update>(new CommandDefinition($"""
            SELECT {Selected}
            FROM dbo.Updates u
            JOIN dbo.Products p ON p.Id = u.ProductId
            WHERE p.BelongsToId = @userId AND (@productId IS NULL OR u.ProductId = @productId)
            ORDER BY u.CreatedAt DESC;
            """, new { userId, productId }, cancellationToken: ct));
        return rows.AsList();
    }

    public async Task<Update?> GetAsync(Guid id, Guid userId, CancellationToken ct = default)
    {
        await using var connection = await db.OpenAsync(ct);
        return await connection.QuerySingleOrDefaultAsync<Update>(new CommandDefinition($"""
            SELECT {Selected}
            FROM dbo.Updates u
            JOIN dbo.Products p ON p.Id = u.ProductId
            WHERE u.Id = @id AND p.BelongsToId = @userId;
            """, new { id, userId }, cancellationToken: ct));
    }

    /// <summary>Returns null when the product does not exist or belongs to someone else.</summary>
    public async Task<Update?> CreateAsync(Guid userId, CreateUpdateRequest request, CancellationToken ct = default)
    {
        await using var connection = await db.OpenAsync(ct);
        return await connection.QuerySingleOrDefaultAsync<Update>(new CommandDefinition($"""
            INSERT INTO dbo.Updates (Title, Body, Status, Version, Asset, ProductId)
            OUTPUT {Inserted}
            SELECT @Title, @Body, @Status, NULLIF(@Version, ''), NULLIF(@Asset, ''), p.Id
            FROM dbo.Products p
            WHERE p.Id = @ProductId AND p.BelongsToId = @userId;
            """,
            new
            {
                userId,
                request.ProductId,
                request.Title,
                request.Body,
                Status = request.Status ?? UpdateStatus.InProgress,
                request.Version,
                request.Asset,
            },
            cancellationToken: ct));
    }

    public async Task<Update?> EditAsync(Guid id, Guid userId, EditUpdateRequest request, CancellationToken ct = default)
    {
        await using var connection = await db.OpenAsync(ct);
        return await connection.QuerySingleOrDefaultAsync<Update>(new CommandDefinition($"""
            UPDATE u SET
                Title   = COALESCE(@Title, u.Title),
                Body    = COALESCE(@Body, u.Body),
                Status  = COALESCE(@Status, u.Status),
                Version = CASE WHEN @Version IS NULL THEN u.Version ELSE NULLIF(@Version, '') END,
                Asset   = CASE WHEN @Asset   IS NULL THEN u.Asset   ELSE NULLIF(@Asset, '')   END
            OUTPUT {Inserted}
            FROM dbo.Updates u
            JOIN dbo.Products p ON p.Id = u.ProductId
            WHERE u.Id = @id AND p.BelongsToId = @userId;
            """,
            new { id, userId, request.Title, request.Body, request.Status, request.Version, request.Asset },
            cancellationToken: ct));
    }

    public async Task<Update?> DeleteAsync(Guid id, Guid userId, CancellationToken ct = default)
    {
        await using var connection = await db.OpenAsync(ct);
        return await connection.QuerySingleOrDefaultAsync<Update>(new CommandDefinition($"""
            DELETE u
            OUTPUT {Deleted}
            FROM dbo.Updates u
            JOIN dbo.Products p ON p.Id = u.ProductId
            WHERE u.Id = @id AND p.BelongsToId = @userId;
            """, new { id, userId }, cancellationToken: ct));
    }
}
