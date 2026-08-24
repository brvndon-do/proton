using Microsoft.Extensions.DependencyInjection;
using Proton.Engine.Core.Interfaces;

namespace Proton.Engine.Indicators;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddProtonIndicatorServices(this IServiceCollection services)
    {
        services.AddSingleton<IIndicatorService, IndicatorService>();

        return services;
    }
}
