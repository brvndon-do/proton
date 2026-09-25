using Microsoft.Extensions.DependencyInjection;
using Proton.Engine.Repl.Commands;

namespace Proton.Engine.Repl;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddProtonReplService(this IServiceCollection services)
    {
        services.AddSingleton<IReplCommand, ExitCommand>();
        services.AddSingleton<IReplCommand, StatusCommand>();

        services.AddHostedService<ReplService>();

        return services;
    }
}
