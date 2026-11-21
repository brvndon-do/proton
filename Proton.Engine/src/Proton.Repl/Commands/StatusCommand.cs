using Proton.Engine.Repl.Models;
using Spectre.Console;

namespace Proton.Engine.Repl.Commands;

public class StatusCommand : IReplCommand
{
    public string Name => "status";
    public string Description => "Displays engine status.";
    public IReadOnlyList<string> Examples => ["status"];

    public Task<(CommandStatus status, string? message)> ExecuteAsync(string[] args, CancellationToken cancellationToken = default)
    {
        // TODO: actually display status..
        AnsiConsole.Write("status\n");
        return Task.FromResult<(CommandStatus, string?)>((CommandStatus.Success, null));
    }
}
