using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using IPop.Infrastructure.Authentication;
using IPop.Infrastructure.Chat;
using IPop.Modules.Auth.Application.Abstractions;
using IPop.Modules.Chat.Application.Abstractions;
using IPop.Modules.Chat.Application.Attachments;
using IPop.Modules.Chat.Application.Content;
using IPop.Modules.Chat.Application.Presence;
using IPop.Shared.Time;
using StackExchange.Redis;

namespace IPop.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var sqlConn = configuration.GetConnectionString("Primary")
            ?? throw new InvalidOperationException("Connection string 'Primary' missing");

        services.AddDbContext<Persistence.AppDbContext>(opt =>
            opt.UseSqlServer(sqlConn, sql => sql.EnableRetryOnFailure(3)));

        var redisConn = configuration.GetConnectionString("Redis")
            ?? throw new InvalidOperationException("Connection string 'Redis' missing");
        services.AddSingleton<IConnectionMultiplexer>(
            _ => ConnectionMultiplexer.Connect(redisConn));

        services.AddSingleton<IClock, SystemClock>();
        services.AddScoped<ILegacyHrRepository, Legacy.LegacySjatrOdbcAdapter>();
        services.AddScoped<IUserRepository, EfUserRepository>();
        services.AddScoped<IAuditWriter, EfAuditWriter>();
        services.AddScoped<IChatRepository, EfChatRepository>();
        services.AddScoped<IChannelRepository, EfChannelRepository>();
        services.AddSingleton<IPresenceService, RedisPresenceService>();
        services.Configure<FileStorageOptions>(configuration.GetSection("FileStorage"));
        services.AddScoped<IFileStorage, LocalFileStorage>();
        services.AddSingleton<IChatContentSanitizer, HtmlAgilityChatContentSanitizer>();

        return services;
    }
}
