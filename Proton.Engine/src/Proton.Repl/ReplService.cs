using Microsoft.Extensions.Hosting;
using Proton.Engine.Repl.Commands;
using Proton.Engine.Repl.Models;
using Spectre.Console;

namespace Proton.Engine.Repl;

public class ReplService(
    IEnumerable<IReplCommand> commands,
    IHostApplicationLifetime applicationLifetime)
    : BackgroundService
{
    private readonly IDictionary<string, IReplCommand> _commands = commands
        .SelectMany(
            x => x.Aliases.Prepend(x.Name),
            (cmd, key) => (Key: key, Command: cmd))
        .ToDictionary(x => x.Key, x => x.Command, StringComparer.OrdinalIgnoreCase);

    private readonly IHostApplicationLifetime _applicationLifetime = applicationLifetime;

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        // no TTY; skip service
        if (Console.IsInputRedirected)
            return;

        using CancellationTokenSource cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _applicationLifetime.ApplicationStopping);

        AnsiConsole.Write(new FigletText("Proton"));

        while (!cts.IsCancellationRequested)
        {
            string line = await AnsiConsole.PromptAsync(
                new TextPrompt<string>("[green]>[/]").AllowEmpty(),
                cts.Token);

            await DispatchAsync(line, cts.Token);
        }
    }

    private async Task DispatchAsync(string line, CancellationToken cancellationToken)
    {
        string[] parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (parts.Length == 0)
            return;

        if (!_commands.TryGetValue(parts[0], out IReplCommand? command))
        {
            AnsiConsole.MarkupLine($"[red]Unknown command:[/] {parts[0].EscapeMarkup()}");
            return;
        }

        try
        {
            (CommandStatus status, string? message) = await command.ExecuteAsync(parts[1..], cancellationToken);

            switch (status)
            {
                // TODO: make prettier
                case CommandStatus.Invalid:
                    AnsiConsole.MarkupLine($"[yellow]{message.EscapeMarkup()}[/]");
                    return;
                case CommandStatus.Error:
                    AnsiConsole.MarkupLine($"[red]Error: {message.EscapeMarkup()}");
                    return;
                case CommandStatus.Success:
                    return;
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            AnsiConsole.WriteException(ex, ExceptionFormats.ShortenEverything);
        }
    }
}
