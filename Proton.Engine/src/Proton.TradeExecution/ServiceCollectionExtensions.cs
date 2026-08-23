using Microsoft.Extensions.DependencyInjection;

namespace Proton.Engine.TradeExecution;

public static class ServiceCollectionExtensions
{
    public static void AddProtonTradeServices(this IServiceCollection services)
    {
        services.AddSingleton<ITradeExecutionService, TradeExecutionService>();
    }
}
