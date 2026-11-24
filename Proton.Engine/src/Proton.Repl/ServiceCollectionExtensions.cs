using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Spectre.Console;
using Proton.Engine.Repl.Commands;

namespace Proton.Engine.Repl;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddProtonReplService(this IServiceCollection services)
    {
        // shared console; tests substitute Spectre's TestConsole
        services.TryAddSingleton(AnsiConsole.Console);

        services.AddSingleton<IReplCommand, HelpCommand>();
        services.AddSingleton<IReplCommand, ExitCommand>();
        services.AddSingleton<IReplCommand, StatusCommand>();
        services.AddSingleton<IReplCommand, SubscribeCommand>();
        services.AddSingleton<IReplCommand, UnsubscribeCommand>();

        services.AddHostedService<ReplService>();

        return services;
    }
}
