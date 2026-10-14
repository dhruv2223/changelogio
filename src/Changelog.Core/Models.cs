namespace Changelog.Core;

public sealed class Product
{
    public Guid Id { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public string Name { get; set; } = "";
    public Guid BelongsToId { get; set; }
}

public sealed class Update
{
    public Guid Id { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public string Title { get; set; } = "";
    public string Body { get; set; } = "";
    public string Status { get; set; } = UpdateStatus.InProgress;
    public string? Version { get; set; }
    public string? Asset { get; set; }
    public Guid ProductId { get; set; }
}

public sealed class UpdatePoint
{
    public Guid Id { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public Guid UpdateId { get; set; }
}

/// <summary>Allowed values for <see cref="Update.Status"/>; mirrored by CK_Updates_Status in schema.sql.</summary>
public static class UpdateStatus
{
    public const string InProgress = "IN_PROGRESS";
    public const string Shipped = "SHIPPED";
    public const string Deprecated = "DEPRECATED";

    public static readonly IReadOnlyList<string> All = [InProgress, Shipped, Deprecated];

    public static bool IsValid(string? value) => value is not null && All.Contains(value);
}
