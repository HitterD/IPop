namespace SJAConnect.Modules.Auth.Application.Commands;

public sealed record LoginResult(
    bool Success,
    string? AccessToken,
    Guid? UserId,
    string? Nik,
    string? DisplayName,
    IReadOnlyCollection<string> Roles,
    string? Error);
