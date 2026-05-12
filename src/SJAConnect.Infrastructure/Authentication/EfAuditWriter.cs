using SJAConnect.Infrastructure.Persistence;
using SJAConnect.Modules.Auth.Application.Abstractions;
using SJAConnect.Modules.Auth.Domain;

namespace SJAConnect.Infrastructure.Authentication;

public sealed class EfAuditWriter(AppDbContext db) : IAuditWriter
{
    public async Task WriteAsync(string eventType, Guid actorUserId, string? actorNik, Guid? targetUserId, string? targetResource, string? payload, string? ipAddress, string? userAgent, CancellationToken cancellationToken)
    {
        var audit = AuditLog.Create(eventType, actorUserId, actorNik, targetUserId, targetResource, payload, ipAddress, userAgent);
        await db.AuditLogs.AddAsync(audit, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
    }
}
