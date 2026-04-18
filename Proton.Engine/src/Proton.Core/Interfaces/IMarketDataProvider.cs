using Proton.Engine.Core.Models;
using Proton.Engine.Core.Models.MarketData;
using Proton.Engine.Core.Models.Trading;

namespace Proton.Engine.Core.Interfaces;

public interface IMarketDataProvider
{
    // market operations
    Task ConnectAsync(CancellationToken cancellationToken = default);
    Task DisconnectAsync(CancellationToken cancellationToken = default);

    // subscription
    Task SubscribeToSymbolAsync(string symbol, CancellationToken cancellationToken = default);
    Task UnsubscribeToSymbolAsync(string symbol, CancellationToken cancellationToken = default);

    // streaming
    IAsyncEnumerable<Bar> StreamBarsAsync(CancellationToken cancellationToken = default);
    IAsyncEnumerable<NewsArticle> StreamNewsDataAsync(CancellationToken cancellationToken = default);

    // fetching
    Task<IEnumerable<NewsArticle>> GetNewsDataAsync(MarketNewsRequest request, CancellationToken cancellationToken = default);
    Task<IEnumerable<Bar>> GetHistoricalBarsAsync(
        IEnumerable<string> symbols,
        TimeFrame timeFrame,
        DateTime? from,
        DateTime? to,
        int limit = 1_000,
        CancellationToken cancellationToken = default
    );
}
