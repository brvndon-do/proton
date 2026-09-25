namespace Proton.Engine.Repl.Commands;

public interface IReplCommand
{
    string Name { get; }
    IReadOnlyList<string> Aliases => [];
    string Description { get; }
    Task ExecuteAsync(string[] args, CancellationToken cancellationToken = default);
}
