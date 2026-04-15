using System.Collections.Concurrent;
using System.Threading.Channels;
using Proton.Engine.Core.Models;

namespace Proton.Engine.MarketDataIngestion.Models;

internal class SymbolSubscription
{
    public required string Symbol { get; set; }
    public int ActiveSubscribersCount;
    public required Task SubscriptionTask { get; set; }
    public required CancellationTokenSource SubscriptionCancellationTokenSource { get; set; }
    public required ConcurrentBag<Channel<Bar>> SubscriberChannels { get; set; }
    public bool IsBackfilling { get; set; }

    public DateTime StartedAtUtc { get; set; }
}
