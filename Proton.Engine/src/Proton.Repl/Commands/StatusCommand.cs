using Spectre.Console;

namespace Proton.Engine.Repl.Commands;

public class StatusCommand : IReplCommand
{
    public string Name => "status";
    public string Description => "Displays engine status.";

    public Task ExecuteAsync(string[] args, CancellationToken cancellationToken = default)
    {
        // TODO: actually display status..
        AnsiConsole.Write("status\n");
        return Task.CompletedTask;
    }
}
