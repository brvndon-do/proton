using Microsoft.Extensions.Logging.Abstractions;
using Proton.Engine.Repl.Commands;
using Proton.Engine.Repl.Models;
using Spectre.Console.Testing;

namespace Proton.Engine.Repl.Tests;

// subscribe and unsubscribe take the same single <symbol> argument, so they share these tests
public class SymbolCommandTests
{
    private readonly TestConsole _console = new();

    public static TheoryData<string> Commands => ["subscribe", "unsubscribe"];

    private IReplCommand Create(string name) => name switch
    {
        "subscribe" => new SubscribeCommand(new StubSubscriptionManager(), NullLogger<SubscribeCommand>.Instance, _console),
        "unsubscribe" => new UnsubscribeCommand(new StubSubscriptionManager(), NullLogger<UnsubscribeCommand>.Instance, _console),
        _ => throw new ArgumentOutOfRangeException(nameof(name)),
    };

    [Theory]
    [MemberData(nameof(Commands))]
    public async Task NoSymbol_ReturnsInvalidWithUsage_InsteadOfThrowing(string name)
    {
        IReplCommand command = Create(name);

        (CommandStatus status, string? message) = await command.ExecuteAsync([]);

        Assert.Equal(CommandStatus.Invalid, status);
        Assert.Contains("Missing symbol", message);
        Assert.Contains(command.Usage, message);
    }

    [Theory]
    [MemberData(nameof(Commands))]
    public async Task TooManyArgs_ReturnsInvalidWithUsage(string name)
    {
        IReplCommand command = Create(name);

        (CommandStatus status, string? message) = await command.ExecuteAsync(["AAPL", "TSLA"]);

        Assert.Equal(CommandStatus.Invalid, status);
        Assert.Contains("Too many arguments", message);
        Assert.Contains(command.Usage, message);
    }

    [Theory]
    [MemberData(nameof(Commands))]
    public async Task OneSymbol_Succeeds(string name)
    {
        IReplCommand command = Create(name);

        (CommandStatus status, string? message) = await command.ExecuteAsync(["AAPL"]);

        Assert.Equal(CommandStatus.Success, status);
        Assert.Null(message);
    }
}
