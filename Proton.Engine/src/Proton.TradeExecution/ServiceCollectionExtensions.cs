using Microsoft.Extensions.DependencyInjection;

namespace Proton.Engine.TradeExecution;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddProtonTradeServices(this IServiceCollection services)
    {
        services.AddSingleton<ITradeExecutionService, TradeExecutionService>();

        return services;
    }
}
