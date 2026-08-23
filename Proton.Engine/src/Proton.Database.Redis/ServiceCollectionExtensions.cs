using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Proton.Engine.Core.Interfaces.Repositories;
using StackExchange.Redis;

namespace Proton.Engine.Database.Redis;

public static class ServiceCollectionExtensions
{
    public static void AddProtonRedisServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<RedisOptions>(configuration.GetSection(RedisOptions.SectionName));

        services.AddSingleton<ICacheRepository, RedisRepository>();
        services.AddSingleton<IConnectionMultiplexer>(
            ConnectionMultiplexer.Connect(configuration["RedisOptions:Configuration"] ?? throw new ArgumentNullException("RedisOptions:Configuration must be set"))
        );
    }
}
