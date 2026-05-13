namespace IPop.Modules.Auth.Application.Abstractions;

public interface IAuditWriter
{
    Task WriteAsync(string eventType, Guid actorUserId, string? actorNik, Guid? targetUserId, string? targetResource, string? payload, string? ipAddress, string? userAgent, CancellationToken cancellationToken);
}
