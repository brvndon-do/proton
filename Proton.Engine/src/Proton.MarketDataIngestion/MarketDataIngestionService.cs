using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Proton.Engine.Core.Interfaces;
using Proton.Engine.Core.Models;

namespace Proton.Engine.MarketDataIngestion;

public class MarketDataIngestion(IMarketDataProvider marketDataProvider, ILogger<MarketDataIngestion> logger) : BackgroundService
{
    private readonly IMarketDataProvider _marketDataProvider = marketDataProvider;
    private readonly ILogger<MarketDataIngestion> _logger = logger;

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        await foreach (Bar bar in _marketDataProvider.StreamBarsAsync(["AAPL", "TSLA"], cancellationToken))
        {
            _logger.LogInformation("Symbol: {s}\nOHLCV: {o}/{h}/{l}/{c}/{v}", bar.Symbol, bar.Open, bar.High, bar.Low, bar.Close, bar.Volume);
        }

        await foreach (NewsArticle article in _marketDataProvider.StreamNewsDataAsync(cancellationToken))
        {
            _logger.LogInformation("News {h}: {s} by {a} from {src}\n", article.Headline, article.Summary, article.Author, article.Source);
        }
    }
}
