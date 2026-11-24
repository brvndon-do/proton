using Microsoft.Extensions.DependencyInjection;
using Proton.Engine.Repl.Models;
using Spectre.Console;

namespace Proton.Engine.Repl.Commands;

// IServiceProvider instead of IEnumerable<IReplCommand>: this class is itself an IReplCommand,
// so injecting the collection would be a circular dependency. commands are resolved per call instead.
public class HelpCommand(IServiceProvider serviceProvider, IAnsiConsole console) : IReplCommand
{
    private readonly IServiceProvider _serviceProvider = serviceProvider;
    private readonly IAnsiConsole _console = console;

    public string Name => "help";
    public IReadOnlyList<string> Aliases => ["?", "h"];
    public string Description => "Lists commands, or shows usage and examples for one command.";
    public string Usage => "help [command]";
    public IReadOnlyList<string> Examples => ["help", "help subscribe"];

    public Task<(CommandStatus status, string? message)> ExecuteAsync(string[] args, CancellationToken cancellationToken = default)
    {
        if (args.Length > 1)
            return Result(CommandStatus.Invalid, $"Too many arguments. Usage: {Usage}");

        IReplCommand[] commands = [.. _serviceProvider.GetServices<IReplCommand>().OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase)];

        if (args.Length == 0)
        {
            WriteOverview(commands);
            return Result(CommandStatus.Success);
        }

        IReplCommand? command = commands.FirstOrDefault(x =>
            x.Name.Equals(args[0], StringComparison.OrdinalIgnoreCase) ||
            x.Aliases.Contains(args[0], StringComparer.OrdinalIgnoreCase));

        if (command is null)
            return Result(CommandStatus.Invalid, $"Unknown command: {args[0]}. Type 'help' to list commands.");

        WriteDetails(command);
        return Result(CommandStatus.Success);
    }

    private void WriteOverview(IEnumerable<IReplCommand> commands)
    {
        Table table = new Table()
            .Border(TableBorder.Rounded)
            .AddColumn("Command")
            .AddColumn("Usage")
            .AddColumn("Description")
            .AddColumn("Examples");

        foreach (IReplCommand command in commands)
        {
            string name = $"[green]{command.Name.EscapeMarkup()}[/]";
            if (command.Aliases.Count > 0)
                name += $" [grey]({string.Join(", ", command.Aliases).EscapeMarkup()})[/]";

            table.AddRow(
                name,
                command.Usage.EscapeMarkup(),
                command.Description.EscapeMarkup(),
                string.Join('\n', command.Examples.Select(x => $"> {x}")).EscapeMarkup());
        }

        _console.Write(table);
        _console.MarkupLine("[grey]Type 'help <command>' for details on a single command.[/]");
    }

    private void WriteDetails(IReplCommand command)
    {
        Grid grid = new Grid().AddColumn(new GridColumn().NoWrap()).AddColumn();

        grid.AddRow("[bold]Usage[/]", command.Usage.EscapeMarkup());
        grid.AddRow("[bold]Description[/]", command.Description.EscapeMarkup());

        if (command.Aliases.Count > 0)
            grid.AddRow("[bold]Aliases[/]", string.Join(", ", command.Aliases).EscapeMarkup());

        if (command.Examples.Count > 0)
            grid.AddRow("[bold]Examples[/]", string.Join('\n', command.Examples.Select(x => $"> {x}")).EscapeMarkup());

        _console.Write(new Panel(grid).Header($"[green]{command.Name.EscapeMarkup()}[/]").Border(BoxBorder.Rounded));
    }

    private static Task<(CommandStatus status, string? message)> Result(CommandStatus status, string? message = null)
        => Task.FromResult<(CommandStatus, string?)>((status, message));
}
