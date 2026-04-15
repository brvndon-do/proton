using System.Threading.Channels;
using Proton.Engine.Core.Models;

namespace Proton.Engine.Core.Interfaces;

public interface IMarketDataSubscriptionManager
{
    // TODO: these are marked as "async" but its implementation actually doesn't contain any await calls. for now, let's leave it. in the future, change the API
    //       to match its "operation"
    Task<Channel<Bar>> SubscribeAsync(string symbol, int subscriberCapacity = 1_000, CancellationToken cancellationToken = default);
    Task UnsubscribeAsync(string symbol, CancellationToken cancellationToken = default);
}
