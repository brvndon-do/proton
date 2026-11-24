using Proton.Engine.Repl.Models;

namespace Proton.Engine.Repl.Commands;

public interface IReplCommand
{
    string Name { get; }
    IReadOnlyList<string> Aliases => [];
    string Description { get; }

    // shown by the help command; defaults let commands opt in
    string Usage => Name;
    IReadOnlyList<string> Examples => [];

    Task<(CommandStatus status, string? message)> ExecuteAsync(string[] args, CancellationToken cancellationToken = default);
}
