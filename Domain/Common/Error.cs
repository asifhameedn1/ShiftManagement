namespace Domain.Common;

/// <summary>
/// Represents a domain error with a code and a human-readable description.
/// </summary>
public sealed record Error(string Code, string Description)
{
    /// <summary>A no-error sentinel used when a Result is successful.</summary>
    public static readonly Error None = new(string.Empty, string.Empty);

    /// <summary>Creates a not-found error.</summary>
    public static Error NotFound(string code, string description) =>
        new($"{code}.NotFound", description);

    /// <summary>Creates a validation error.</summary>
    public static Error Validation(string code, string description) =>
        new($"{code}.Validation", description);

    /// <summary>Creates a conflict error.</summary>
    public static Error Conflict(string code, string description) =>
        new($"{code}.Conflict", description);
}
