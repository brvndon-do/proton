using Microsoft.Extensions.DependencyInjection;
using Proton.Engine.Core.Interfaces.Repositories;

namespace Proton.Engine.Database.Parquet;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddProtonParquetServices(this IServiceCollection services)
    {
        services.AddSingleton<IBarRepository, ParquetRepository>();

        return services;
    }
}
