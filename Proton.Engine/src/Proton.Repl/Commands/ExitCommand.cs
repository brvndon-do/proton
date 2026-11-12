using Microsoft.Extensions.Hosting;
using Proton.Engine.Repl.Models;

namespace Proton.Engine.Repl.Commands;

public class ExitCommand(IHostApplicationLifetime applicationLifetime) : IReplCommand
{
    private readonly IHostApplicationLifetime _applicationLifetime = applicationLifetime;

    public string Name => "exit";
    public IReadOnlyList<string> Aliases => ["quit", "q"];
    public string Description => "Stops the application.";

    public Task<(CommandStatus status, string? message)> ExecuteAsync(string[] args, CancellationToken cancellationToken = default)
    {
        _applicationLifetime.StopApplication();

        return Task.FromResult<(CommandStatus, string?)>((CommandStatus.Success, null));
    }
}
