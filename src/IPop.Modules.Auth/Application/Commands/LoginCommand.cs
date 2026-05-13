using MediatR;

namespace SJAConnect.Modules.Auth.Application.Commands;

public sealed record LoginCommand(string Nik, string Password, string? IpAddress, string? UserAgent) : IRequest<LoginResult>;
