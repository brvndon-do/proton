using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Proton.Engine.Core.Interfaces;
using Proton.Engine.Repl.Commands;

namespace Proton.Engine.Repl.Tests;

// exercises the real AddProtonReplService registration, so these catch mistakes in how commands are wired up
public class ReplRegistrationTests
{
    private static ServiceProvider BuildProvider()
    {
        ServiceCollection services = new();
        services.AddLogging();
        services.AddSingleton<IMarketDataSubscriptionManager, StubSubscriptionManager>();
        services.AddSingleton<IHostApplicationLifetime, StubApplicationLifetime>();
        services.AddProtonReplService();

        // ValidateOnBuild fails here (instead of at app startup) on missing or circular dependencies
        return services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
    }

    private static IReplCommand[] Commands() => [.. BuildProvider().GetServices<IReplCommand>()];

    [Fact]
    public void AllCommandsResolve_IncludingHelp()
    {
        IReplCommand[] commands = Commands();

        Assert.Contains(commands, x => x is HelpCommand);
        Assert.Contains(commands, x => x is ExitCommand);
    }

    [Fact]
    public void NamesAndAliases_AreUnique()
    {
        // ReplService builds a case-insensitive dictionary of these and would throw on startup if two collide
        string[] keys = [.. Commands().SelectMany(x => x.Aliases.Prepend(x.Name))];

        string[] duplicates = [.. keys
            .GroupBy(x => x, StringComparer.OrdinalIgnoreCase)
            .Where(x => x.Count() > 1)
            .Select(x => x.Key)];

        Assert.Empty(duplicates);
    }

    [Fact]
    public void EveryCommand_IsDocumentedForHelp()
    {
        foreach (IReplCommand command in Commands())
        {
            Assert.False(string.IsNullOrWhiteSpace(command.Description), $"'{command.Name}' has no description");
            Assert.NotEmpty(command.Examples);
        }
    }

    [Fact]
    public void EveryExample_StartsWithThatCommandsNameOrAlias()
    {
        // keeps examples honest: an example must be something you could actually type
        foreach (IReplCommand command in Commands())
        {
            string[] keys = [command.Name, .. command.Aliases];

            foreach (string example in command.Examples)
            {
                string first = example.Split(' ', StringSplitOptions.RemoveEmptyEntries)[0];
                Assert.True(keys.Contains(first, StringComparer.OrdinalIgnoreCase), $"Example '{example}' doesn't invoke '{command.Name}'");
            }
        }
    }
}
