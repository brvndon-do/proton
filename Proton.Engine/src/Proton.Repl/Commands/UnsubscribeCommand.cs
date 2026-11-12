using Microsoft.Extensions.Logging;
using Proton.Engine.Core.Interfaces;
using Proton.Engine.Repl.Models;
using Spectre.Console;

namespace Proton.Engine.Repl.Commands;

public class UnsubscribeCommand(IMarketDataSubscriptionManager marketDataSubscriptionManager, ILogger<UnsubscribeCommand> logger) : IReplCommand
{
    private readonly IMarketDataSubscriptionManager _marketDataSubscriptionManager = marketDataSubscriptionManager;
    private readonly ILogger<UnsubscribeCommand> _logger = logger;

    public string Name => "unsubscribe";
    public string Description => "Unsubscribes from a symbol.";

    public async Task<(CommandStatus status, string? message)> ExecuteAsync(string[] args, CancellationToken cancellationToken = default)
    {
        if (args.Length > 1)
            return (status: CommandStatus.Invalid, message: "Too many arguments!");

        string symbol = args[0];

        try
        {
            // await _marketDataSubscriptionManager.UnsubscribeAsync(symbol, cancellationToken);
            AnsiConsole.MarkupLine($"[green]hello world2[/]");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
            return (status: CommandStatus.Error, null);
        }

        return (status: CommandStatus.Success, null);
    }
}
