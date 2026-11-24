using System.Threading.Channels;
using Microsoft.Extensions.Hosting;
using Proton.Engine.Core.Interfaces;
using Proton.Engine.Core.Models;
using Proton.Engine.Repl.Commands;
using Proton.Engine.Repl.Models;

namespace Proton.Engine.Repl.Tests;

// hand-written test doubles; small enough that a mocking library isn't worth adding yet

internal sealed class FakeCommand(
    string name,
    string description = "A fake command.",
    string? usage = null,
    IReadOnlyList<string>? aliases = null,
    IReadOnlyList<string>? examples = null)
    : IReplCommand
{
    public string Name => name;
    public string Description => description;
    public string Usage => usage ?? name;
    public IReadOnlyList<string> Aliases => aliases ?? [];
    public IReadOnlyList<string> Examples => examples ?? [];

    public Task<(CommandStatus status, string? message)> ExecuteAsync(string[] args, CancellationToken cancellationToken = default)
        => Task.FromResult<(CommandStatus, string?)>((CommandStatus.Success, null));
}

// satisfies Subscribe/UnsubscribeCommand's constructor; never called by these tests
internal sealed class StubSubscriptionManager : IMarketDataSubscriptionManager
{
    public Task PinAsync(string symbol, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    public Task UnpinAsync(string symbol, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    public Task<Channel<Bar>> SubscribeAsync(string symbol, int subscriberCapacity = 1_000, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    public Task UnsubscribeAsync(string symbol, CancellationToken cancellationToken = default) => throw new NotSupportedException();
}

// satisfies ExitCommand/ReplService's constructor
internal sealed class StubApplicationLifetime : IHostApplicationLifetime
{
    public CancellationToken ApplicationStarted => CancellationToken.None;
    public CancellationToken ApplicationStopping => CancellationToken.None;
    public CancellationToken ApplicationStopped => CancellationToken.None;
    public void StopApplication() { }
}
