using IPop.Modules.Chat.Application.Presence;
using StackExchange.Redis;

namespace IPop.Infrastructure.Chat;

public sealed class RedisPresenceService(IConnectionMultiplexer redis) : IPresenceService
{
    private static readonly TimeSpan PresenceTtl = TimeSpan.FromSeconds(90);

    public async Task SetAsync(Guid userId, PresenceState state, CancellationToken cancellationToken)
    {
        if (userId == Guid.Empty || state == PresenceState.Offline)
        {
            await ClearAsync(userId, cancellationToken);
            return;
        }

        try
        {
            var database = redis.GetDatabase();
            await database.StringSetAsync(Key(userId), ToRedisValue(state), PresenceTtl);
        }
        catch (RedisException)
        {
        }
        catch (RedisTimeoutException)
        {
        }
    }

    public async Task<IReadOnlyDictionary<Guid, PresenceState>> GetManyAsync(IReadOnlyCollection<Guid> userIds, CancellationToken cancellationToken)
    {
        var offline = userIds.Distinct().ToDictionary(userId => userId, _ => PresenceState.Offline);
        if (offline.Count == 0)
        {
            return offline;
        }

        try
        {
            var database = redis.GetDatabase();
            var keys = offline.Keys.Select(Key).ToArray();
            var values = await database.StringGetAsync(keys);
            var states = offline.Keys.Zip(values, (userId, value) => new { userId, value })
                .ToDictionary(x => x.userId, x => FromRedisValue(x.value));
            return states;
        }
        catch (RedisException)
        {
            return offline;
        }
        catch (RedisTimeoutException)
        {
            return offline;
        }
    }

    public async Task ClearAsync(Guid userId, CancellationToken cancellationToken)
    {
        if (userId == Guid.Empty)
        {
            return;
        }

        try
        {
            var database = redis.GetDatabase();
            await database.KeyDeleteAsync(Key(userId));
        }
        catch (RedisException)
        {
        }
        catch (RedisTimeoutException)
        {
        }
    }

    private static RedisKey Key(Guid userId) => $"presence:{userId}";

    private static RedisValue ToRedisValue(PresenceState state) => state switch
    {
        PresenceState.Online => "online",
        PresenceState.Away => "away",
        _ => RedisValue.Null
    };

    private static PresenceState FromRedisValue(RedisValue value) => value.ToString() switch
    {
        "online" => PresenceState.Online,
        "away" => PresenceState.Away,
        _ => PresenceState.Offline
    };
}
