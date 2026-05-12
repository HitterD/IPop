namespace SJAConnect.Infrastructure.Legacy;

public sealed record LegacyEmployee(
    string NikHris,
    string Name,
    string? Department,
    string? Position,
    string? Location,
    string? NikSantos,
    string? Md5Keyword);
