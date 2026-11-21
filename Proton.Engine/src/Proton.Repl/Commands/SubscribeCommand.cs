using System.Dynamic;
using Microsoft.Extensions.Logging;
using Proton.Engine.Core.Interfaces;
using Proton.Engine.Repl.Models;
using Spectre.Console;

namespace Proton.Engine.Repl.Commands;

public class SubscribeCommand(
    IMarketDataSubscriptionManager marketDataSubscriptionManager,
    ILogger<SubscribeCommand> logger)
    : IReplCommand
{
    private readonly IMarketDataSubscriptionManager _marketDataSubscriptionManager = marketDataSubscriptionManager;
    private readonly ILogger<SubscribeCommand> _logger = logger;

    public string Name => "subscribe";
    public string Description => "Subscribes to a symbol.";
    public string Usage => "subscribe <symbol>";
    public IReadOnlyList<string> Examples => ["subscribe AAPL"];

    public async Task<(CommandStatus status, string? message)> ExecuteAsync(string[] args, CancellationToken cancellationToken)
    {
        if (args.Length > 1)
            return (status: CommandStatus.Invalid, message: "Too many arguments!");

        string symbol = args[0];

        try
        {
            // await _marketDataSubscriptionManager.SubscribeAsync(symbol, cancellationToken: cancellationToken);
            AnsiConsole.MarkupLine("[green]hello world[/]");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
            return (status: CommandStatus.Error, ex.Message); // TODO: better exception message for when SubscribeAsync is thrown
        }

        return (status: CommandStatus.Success, null);
    }
}
