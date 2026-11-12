using Proton.Engine.Repl.Models;

namespace Proton.Engine.Repl.Commands;

public interface IReplCommand
{
    string Name { get; }
    IReadOnlyList<string> Aliases => [];
    string Description { get; }
    Task<(CommandStatus status, string? message)> ExecuteAsync(string[] args, CancellationToken cancellationToken = default);
}
