using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Proton.Engine.Core.Interfaces;
using Proton.Engine.Core.Models;
using Proton.Engine.Core.Models.MarketData;

namespace Proton.Engine.MarketDataIngestion;

public class MarketDataIngestion(
    IMarketDataProvider marketDataProvider,
    IChannelManager channelManager,
    IMarketDataSubscriptionManager marketDataSubscriptionManager,
    ILogger<MarketDataIngestion> logger
) : BackgroundService
{
    private readonly IMarketDataProvider _marketDataProvider = marketDataProvider;
    private readonly IChannelManager _channelManager = channelManager;
    private readonly IMarketDataSubscriptionManager _marketDataSubscriptionManager = marketDataSubscriptionManager;
    private readonly ILogger<MarketDataIngestion> _logger = logger;

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        await Task.WhenAll(
            ProcessWarmupSymbolSubscriptions(cancellationToken),
            ProcessMarketNewsContextsAsync(cancellationToken)
        );
    }

    private async Task ProcessWarmupSymbolSubscriptions(CancellationToken cancellationToken)
    {
        _logger.LogDebug("Processing warmup symbols...");

        // TODO: read from different source instead of hard code
        string[] symbols = ["AAPL", "TSLA", "NVDA", "META"];

        foreach (string symbol in symbols)
            await _marketDataSubscriptionManager.SubscribeAsync(symbol, cancellationToken: cancellationToken);
    }

    private async Task ProcessMarketNewsContextsAsync(CancellationToken cancellationToken)
    {
        _logger.LogDebug("Processing news data...");

        await foreach (MarketNewsContext context in _channelManager.MarketNewsContextChannel.Reader.ReadAllAsync(cancellationToken))
        {
            await foreach (NewsArticle article in _marketDataProvider.StreamNewsDataAsync(cancellationToken))
            {
                try
                {
                    await context.MarketNewsResponseChannel.Writer.WriteAsync(new MarketNewsSnapshot
                    {
                        Headline = article.Headline,
                        Summary = article.Summary,
                        Source = article.Source,
                        CreatedAtUtc = article.CreatedAtUtc,
                        Symbols = article.Symbols
                    }, cancellationToken);
                }
                catch (OperationCanceledException ex)
                {
                    _logger.LogError(ex.Message);
                }
            }

            // TODO: redis cache implementation here
        }
    }
}
