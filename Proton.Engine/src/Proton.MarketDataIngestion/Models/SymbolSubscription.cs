using System.Collections.Concurrent;
using System.Threading.Channels;
using Proton.Engine.Core.Models;

namespace Proton.Engine.MarketDataIngestion.Models;

internal class SymbolSubscription
{
    public required string Symbol { get; set; }
    public int ActiveSubscribersCount;
    public int PinCount;
    public required ConcurrentBag<Channel<Bar>> SubscriberChannels { get; set; }

    public DateTime StartedAtUtc { get; set; }

    // Guards one-time async initialization (backfill + provider subscribe) so it runs
    // exactly once per symbol, even under concurrent first-subscribe races.
    public Lazy<Task> Initialization { get; set; } = null!;
}
