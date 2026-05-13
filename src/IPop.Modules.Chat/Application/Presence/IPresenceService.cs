namespace IPop.Modules.Chat.Application.Presence;

public enum PresenceState
{
    Offline = 0,
    Online = 1,
    Away = 2
}

public interface IPresenceService
{
    Task SetAsync(Guid userId, PresenceState state, CancellationToken cancellationToken);
    Task<IReadOnlyDictionary<Guid, PresenceState>> GetManyAsync(IReadOnlyCollection<Guid> userIds, CancellationToken cancellationToken);
    Task ClearAsync(Guid userId, CancellationToken cancellationToken);
}
