using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SJAConnect.Shared.Time;
using StackExchange.Redis;

namespace SJAConnect.Infrastructure;

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

        return services;
    }
}
