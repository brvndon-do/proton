using Microsoft.Extensions.DependencyInjection;
using Proton.Engine.Core.Interfaces.Repositories;

namespace Proton.Engine.Database.Parquet;

public static class ServiceCollectionExtensions
{
    public static void AddProtonParquetServices(this IServiceCollection services)
    {
        services.AddSingleton<IBarRepository, ParquetRepository>();
    }
}
