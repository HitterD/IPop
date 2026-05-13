namespace IPop.Modules.Auth.Application.Abstractions;

public sealed record LegacyEmployee(
    string NikHris,
    string Name,
    string? Department,
    string? Position,
    string? Location,
    string? NikSantos,
    string? Md5Keyword);
