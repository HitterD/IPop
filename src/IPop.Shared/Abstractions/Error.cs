namespace SJAConnect.Shared.Abstractions;

public sealed record Error(string Code, string Message)
{
    public static readonly Error None = new(string.Empty, string.Empty);
    public static Error Unauthorized => new("auth.unauthorized", "Unauthorized");
    public static Error Forbidden => new("auth.forbidden", "Forbidden");
    public static Error NotFound(string what) => new("not_found", $"{what} not found");
    public static Error Validation(string msg) => new("validation", msg);
}
