using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Proton.Engine.Core.Interfaces;
using Proton.Engine.Core.Models;
using Proton.Engine.Core.Models.MarketData;

namespace Proton.Engine.MarketDataIngestion;

public class MarketDataIngestion(
    IMarketDataProvider marketDataProvider,
    IChannelManager channelManager,
    ILogger<MarketDataIngestion> logger
) : BackgroundService
{
    private readonly IMarketDataProvider _marketDataProvider = marketDataProvider;
    private readonly IChannelManager _channelManager = channelManager;
    private readonly ILogger<MarketDataIngestion> _logger = logger;

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        _ = Task.Run(async () =>
        {
            await foreach (MarketDataContext context in _channelManager.MarketDataContextChannel.Reader.ReadAllAsync())
            {
                _ = Task.Run(async () =>
                {
                    await foreach (Bar bar in _marketDataProvider.StreamBarsAsync(context.Request.Symbols, cancellationToken))
                    {
                        await context.MarketDataChannel.Writer.WriteAsync(new MarketDataSnapshot
                        {
                            Symbol = bar.Symbol,
                            TimestampUtc = bar.DateTimeUtc,
                            Open = bar.Open,
                            High = bar.High,
                            Low = bar.Low,
                            Close = bar.Close,
                            Volume = bar.Volume,
                            Indicators = new Dictionary<string, decimal>(), // TODO: indicators...
                        }, cancellationToken);
                    }
                });
            }
        });

        _ = Task.Run(async () =>
        {
            await foreach (MarketNewsContext context in _channelManager.MarketNewsContextChannel.Reader.ReadAllAsync(cancellationToken))
            {
                _ = Task.Run(async () =>
                {
                    await foreach (NewsArticle article in _marketDataProvider.StreamNewsDataAsync(cancellationToken))
                    {
                        await context.MarketNewsChannel.Writer.WriteAsync(new MarketNewsSnapshot
                        {
                            Headline = article.Headline,
                            Summary = article.Summary,
                            Source = article.Source,
                            CreatedAtUtc = article.CreatedAtUtc,
                            Symbols = article.Symbols
                        }, cancellationToken);
                    }
                });
            }
        });
    }
}
