using MediatR;

namespace IPop.Modules.Auth.Application.Commands;

public sealed record LoginCommand(string Nik, string Password, string? IpAddress, string? UserAgent) : IRequest<LoginResult>;
