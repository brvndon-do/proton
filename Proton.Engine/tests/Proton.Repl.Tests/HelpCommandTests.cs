using Microsoft.Extensions.DependencyInjection;
using Proton.Engine.Repl.Commands;
using Proton.Engine.Repl.Models;
using Spectre.Console;
using Spectre.Console.Testing;

namespace Proton.Engine.Repl.Tests;

public class HelpCommandTests
{
    private readonly TestConsole _console = new();

    // builds a DI container holding HelpCommand plus the given fake commands, just like the real app does
    private HelpCommand CreateHelp(params IReplCommand[] others)
    {
        _console.Profile.Width = 200; // wide enough that table cells don't wrap mid-word

        ServiceCollection services = new();
        services.AddSingleton<IAnsiConsole>(_console);
        services.AddSingleton<IReplCommand, HelpCommand>();
        foreach (IReplCommand command in others)
            services.AddSingleton(command);

        return services.BuildServiceProvider()
            .GetServices<IReplCommand>()
            .OfType<HelpCommand>()
            .Single();
    }

    [Fact]
    public async Task NoArgs_ListsEveryCommandWithUsageDescriptionAndExamples()
    {
        HelpCommand help = CreateHelp(
            new FakeCommand("subscribe", "Subscribes to a symbol.", "subscribe <symbol>", examples: ["subscribe AAPL"]),
            new FakeCommand("exit", "Stops the application.", aliases: ["quit", "q"]));

        (CommandStatus status, string? message) = await help.ExecuteAsync([]);

        Assert.Equal(CommandStatus.Success, status);
        Assert.Null(message);
        Assert.Contains("subscribe <symbol>", _console.Output);
        Assert.Contains("Subscribes to a symbol.", _console.Output);
        Assert.Contains("> subscribe AAPL", _console.Output);
        Assert.Contains("(quit, q)", _console.Output);
    }

    [Fact]
    public async Task NoArgs_IncludesHelpItself()
    {
        HelpCommand help = CreateHelp();

        await help.ExecuteAsync([]);

        Assert.Contains("help [command]", _console.Output);
    }

    [Fact]
    public async Task NoArgs_ListsCommandsAlphabetically()
    {
        HelpCommand help = CreateHelp(new FakeCommand("zeta"), new FakeCommand("alpha"));

        await help.ExecuteAsync([]);

        string output = _console.Output;
        Assert.True(output.IndexOf("alpha") < output.IndexOf("help [command]"));
        Assert.True(output.IndexOf("help [command]") < output.IndexOf("zeta"));
    }

    [Theory]
    [InlineData("subscribe")]
    [InlineData("SUBSCRIBE")] // case-insensitive, matching ReplService's dispatch
    [InlineData("sub")]       // alias
    public async Task WithCommand_ShowsDetailsForThatCommandOnly(string arg)
    {
        HelpCommand help = CreateHelp(
            new FakeCommand("subscribe", "Subscribes to a symbol.", "subscribe <symbol>", aliases: ["sub"], examples: ["subscribe AAPL", "subscribe TSLA"]),
            new FakeCommand("exit", "Stops the application."));

        (CommandStatus status, _) = await help.ExecuteAsync([arg]);

        Assert.Equal(CommandStatus.Success, status);
        Assert.Contains("subscribe <symbol>", _console.Output);
        Assert.Contains("> subscribe AAPL", _console.Output);
        Assert.Contains("> subscribe TSLA", _console.Output);
        Assert.DoesNotContain("Stops the application.", _console.Output);
    }

    [Fact]
    public async Task UnknownCommand_ReturnsInvalidAndPrintsNothing()
    {
        HelpCommand help = CreateHelp();

        (CommandStatus status, string? message) = await help.ExecuteAsync(["nope"]);

        Assert.Equal(CommandStatus.Invalid, status);
        Assert.Contains("nope", message);
        Assert.Empty(_console.Output);
    }

    [Fact]
    public async Task TooManyArgs_ReturnsInvalidWithUsage()
    {
        HelpCommand help = CreateHelp();

        (CommandStatus status, string? message) = await help.ExecuteAsync(["a", "b"]);

        Assert.Equal(CommandStatus.Invalid, status);
        Assert.Contains("help [command]", message);
    }

    [Fact]
    public async Task MarkupCharactersInCommandText_AreEscaped()
    {
        // Spectre treats [..] as markup; unescaped text like this would throw
        HelpCommand help = CreateHelp(new FakeCommand("weird", "Uses [brackets] and [/] tags."));

        await help.ExecuteAsync([]);

        Assert.Contains("Uses [brackets] and [/] tags.", _console.Output);
    }
}
