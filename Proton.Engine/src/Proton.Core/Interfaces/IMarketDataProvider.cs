using Proton.Engine.Core.Models;
using Proton.Engine.Core.Models.MarketData;
using Proton.Engine.Core.Models.Trading;

namespace Proton.Engine.Core.Interfaces;

public interface IMarketDataProvider
{
    // streaming data
    IAsyncEnumerable<Bar> StreamBarsAsync(IEnumerable<string> symbols, CancellationToken cancellationToken = default);
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
