using Changelog.Core;
using Dapper;

namespace Changelog.Api.Data;

/// <summary>Update points are owned through update -> product; every query joins both to enforce that.</summary>
public sealed class UpdatePointRepository(SqlConnectionFactory db)
{
    private const string Inserted = "INSERTED.Id, INSERTED.CreatedAt, INSERTED.UpdatedAt, INSERTED.Name, INSERTED.Description, INSERTED.UpdateId";
    private const string Deleted = "DELETED.Id, DELETED.CreatedAt, DELETED.UpdatedAt, DELETED.Name, DELETED.Description, DELETED.UpdateId";
    private const string Selected = "up.Id, up.CreatedAt, up.UpdatedAt, up.Name, up.Description, up.UpdateId";
    private const string OwnerJoin = """
        JOIN dbo.Updates u ON u.Id = up.UpdateId
        JOIN dbo.Products p ON p.Id = u.ProductId
        """;

    public async Task<IReadOnlyList<UpdatePoint>> ListAsync(Guid userId, Guid? updateId, CancellationToken ct = default)
    {
        await using var connection = await db.OpenAsync(ct);
        var rows = await connection.QueryAsync<UpdatePoint>(new CommandDefinition($"""
            SELECT {Selected}
            FROM dbo.UpdatePoints up
            {OwnerJoin}
            WHERE p.BelongsToId = @userId AND (@updateId IS NULL OR up.UpdateId = @updateId)
            ORDER BY up.CreatedAt;
            """, new { userId, updateId }, cancellationToken: ct));
        return rows.AsList();
    }

    public async Task<UpdatePoint?> GetAsync(Guid id, Guid userId, CancellationToken ct = default)
    {
        await using var connection = await db.OpenAsync(ct);
        return await connection.QuerySingleOrDefaultAsync<UpdatePoint>(new CommandDefinition($"""
            SELECT {Selected}
            FROM dbo.UpdatePoints up
            {OwnerJoin}
            WHERE up.Id = @id AND p.BelongsToId = @userId;
            """, new { id, userId }, cancellationToken: ct));
    }

    /// <summary>Returns null when the update does not exist or belongs to someone else.</summary>
    public async Task<UpdatePoint?> CreateAsync(Guid userId, CreateUpdatePointRequest request, CancellationToken ct = default)
    {
        await using var connection = await db.OpenAsync(ct);
        return await connection.QuerySingleOrDefaultAsync<UpdatePoint>(new CommandDefinition($"""
            INSERT INTO dbo.UpdatePoints (Name, Description, UpdateId)
            OUTPUT {Inserted}
            SELECT @Name, @Description, u.Id
            FROM dbo.Updates u
            JOIN dbo.Products p ON p.Id = u.ProductId
            WHERE u.Id = @UpdateId AND p.BelongsToId = @userId;
            """, new { userId, request.UpdateId, request.Name, request.Description }, cancellationToken: ct));
    }

    public async Task<UpdatePoint?> EditAsync(Guid id, Guid userId, EditUpdatePointRequest request, CancellationToken ct = default)
    {
        await using var connection = await db.OpenAsync(ct);
        return await connection.QuerySingleOrDefaultAsync<UpdatePoint>(new CommandDefinition($"""
            UPDATE up SET
                Name        = COALESCE(@Name, up.Name),
                Description = COALESCE(@Description, up.Description),
                UpdatedAt   = SYSUTCDATETIME()
            OUTPUT {Inserted}
            FROM dbo.UpdatePoints up
            {OwnerJoin}
            WHERE up.Id = @id AND p.BelongsToId = @userId;
            """, new { id, userId, request.Name, request.Description }, cancellationToken: ct));
    }

    public async Task<UpdatePoint?> DeleteAsync(Guid id, Guid userId, CancellationToken ct = default)
    {
        await using var connection = await db.OpenAsync(ct);
        return await connection.QuerySingleOrDefaultAsync<UpdatePoint>(new CommandDefinition($"""
            DELETE up
            OUTPUT {Deleted}
            FROM dbo.UpdatePoints up
            {OwnerJoin}
            WHERE up.Id = @id AND p.BelongsToId = @userId;
            """, new { id, userId }, cancellationToken: ct));
    }
}
