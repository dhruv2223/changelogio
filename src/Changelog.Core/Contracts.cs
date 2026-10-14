using System.ComponentModel.DataAnnotations;

namespace Changelog.Core;

// Response envelopes (same shapes as the original Express API).

public sealed class DataResponse<T>
{
    public T Data { get; set; } = default!;
}

public sealed class TokenResponse
{
    public string Token { get; set; } = "";
}

public sealed class MessageResponse
{
    public string Message { get; set; } = "";
}

// Requests

public sealed class AuthRequest
{
    [Required, StringLength(100, MinimumLength = 3)]
    public string Username { get; set; } = "";

    [Required, StringLength(200, MinimumLength = 6)]
    public string Password { get; set; } = "";
}

public sealed class ProductRequest
{
    [Required, StringLength(255)]
    public string Name { get; set; } = "";
}

public sealed class CreateUpdateRequest
{
    [Required]
    public Guid? ProductId { get; set; }

    [Required, StringLength(255)]
    public string Title { get; set; } = "";

    [Required]
    public string Body { get; set; } = "";

    [ValidUpdateStatus]
    public string? Status { get; set; }

    [StringLength(50)]
    public string? Version { get; set; }

    [StringLength(2048)]
    public string? Asset { get; set; }
}

/// <summary>Partial edit: null fields are left unchanged; an empty Version/Asset clears it.</summary>
public sealed class EditUpdateRequest
{
    [StringLength(255, MinimumLength = 1)]
    public string? Title { get; set; }

    public string? Body { get; set; }

    [ValidUpdateStatus]
    public string? Status { get; set; }

    [StringLength(50)]
    public string? Version { get; set; }

    [StringLength(2048)]
    public string? Asset { get; set; }
}

public sealed class CreateUpdatePointRequest
{
    [Required]
    public Guid? UpdateId { get; set; }

    [Required, StringLength(255)]
    public string Name { get; set; } = "";

    [Required]
    public string Description { get; set; } = "";
}

/// <summary>Partial edit: null fields are left unchanged.</summary>
public sealed class EditUpdatePointRequest
{
    [StringLength(255, MinimumLength = 1)]
    public string? Name { get; set; }

    public string? Description { get; set; }
}

[AttributeUsage(AttributeTargets.Property)]
public sealed class ValidUpdateStatusAttribute : ValidationAttribute
{
    public ValidUpdateStatusAttribute()
        : base($"Status must be one of: {string.Join(", ", UpdateStatus.All)}.") { }

    public override bool IsValid(object? value) => value is null || UpdateStatus.IsValid(value as string);
}
