using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Proton.Engine.Core.Interfaces;

namespace Proton.Engine.Brokers.Alpaca;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddProtonAlpacaBrokerServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<AlpacaOptions>(configuration.GetSection(AlpacaOptions.SectionName));

        services.AddSingleton<IOrderGateway, AlpacaBroker>();
        services.AddSingleton<IAccountProvider, AlpacaBroker>();
        services.AddSingleton<IMarketClock, AlpacaBroker>();

        services.AddSingleton<IMarketDataProvider, AlpacaMarketDataProvider>();

        return services;
    }
}
