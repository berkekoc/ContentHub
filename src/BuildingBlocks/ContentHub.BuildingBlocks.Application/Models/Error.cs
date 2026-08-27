namespace ContentHub.BuildingBlocks.Application.Models;

/// <summary>Makine-okur kod + insan-okur mesaj taşıyan iş hatası.</summary>
public sealed record Error(string Code, string Message)
{
    public static readonly Error None = new(string.Empty, string.Empty);

    public static Error Validation(string message) => new("validation", message);

    public static Error NotFound(string message) => new("not_found", message);

    public static Error Conflict(string message) => new("conflict", message);
}
