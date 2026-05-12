namespace SJAConnect.Modules.Auth.Domain;

public sealed class AuditLog
{
    private AuditLog() { }
    public long Id { get; private set; }
    public string EventType { get; private set; } = string.Empty;
    public Guid ActorUserId { get; private set; }
    public string? ActorNik { get; private set; }
    public Guid? TargetUserId { get; private set; }
    public string? TargetResource { get; private set; }
    public string? Payload { get; private set; }
    public string? IpAddress { get; private set; }
    public string? UserAgent { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    public static AuditLog Create(string eventType, Guid actorUserId, string? actorNik, Guid? targetUserId, string? targetResource, string? payload, string? ipAddress, string? userAgent)
    {
        return new AuditLog
        {
            EventType = eventType,
            ActorUserId = actorUserId,
            ActorNik = actorNik,
            TargetUserId = targetUserId,
            TargetResource = targetResource,
            Payload = payload,
            IpAddress = ipAddress,
            UserAgent = userAgent,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }
}
