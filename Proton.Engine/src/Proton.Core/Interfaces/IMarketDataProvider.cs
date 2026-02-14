using Proton.Engine.Core.Models;

namespace Proton.Engine.Core.Interfaces;

public interface IMarketDataProvider
{
    IAsyncEnumerable<Bar> StreamBarsAsync(IEnumerable<string> symbols, CancellationToken cancellationToken = default);
    IAsyncEnumerable<NewsArticle> StreamNewsDataAsync(CancellationToken cancellationToken = default);
}
