using Microsoft.Extensions.Hosting;

namespace Proton.Engine.Repl.Commands;

public class ExitCommand(IHostApplicationLifetime applicationLifetime) : IReplCommand
{
    private readonly IHostApplicationLifetime _applicationLifetime = applicationLifetime;

    public string Name => "exit";
    public IReadOnlyList<string> Aliases => ["quit", "q"];
    public string Description => "Stops the application.";

    public Task ExecuteAsync(string[] args, CancellationToken cancellationToken = default)
    {
        _applicationLifetime.StopApplication();
        return Task.CompletedTask;
    }
}
