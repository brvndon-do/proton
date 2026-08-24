using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Proton.Engine.Core.Interfaces.Repositories;
using StackExchange.Redis;

namespace Proton.Engine.Database.Redis;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddProtonRedisServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<RedisOptions>()
            .Bind(configuration.GetSection(RedisOptions.SectionName))
            .Validate(x => !string.IsNullOrWhiteSpace(x.Configuration), "RedisOptions:Configuration must be set")
            .ValidateOnStart();

        services.AddSingleton<IConnectionMultiplexer>(x =>
        {
            RedisOptions options = x.GetRequiredService<IOptions<RedisOptions>>().Value;
            return ConnectionMultiplexer.Connect(options.Configuration, x =>
            {
                x.AbortOnConnectFail = false;
            });
        });

        services.AddSingleton<ICacheRepository, RedisRepository>();

        return services;
    }
}
