using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Proton.Engine.Core.Interfaces;

namespace Proton.Engine.MarketDataIngestion;

public static class ServiceCollectionExtensions
{
    public static void AddProtonMarketDataIngestionServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<MarketDataIngestionOptions>(configuration.GetSection(MarketDataIngestionOptions.SectionName));

        services.AddSingleton<IMarketDataSubscriptionManager, MarketDataSubscriptionManager>();

        services.AddHostedService<MarketDataIngestion>();
    }
}
