using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Proton.Engine.Core.Interfaces;
using Proton.Engine.Core.Interfaces.Repositories;
using Proton.Engine.Core.Models;
using Proton.Engine.Core.Models.MarketData;

namespace Proton.Engine.MarketDataIngestion;

public class MarketDataIngestion(
    IMarketDataProvider marketDataProvider,
    IChannelManager channelManager,
    IBarRepository barRepository,
    ILogger<MarketDataIngestion> logger
) : BackgroundService
{
    private const int LIST_BATCH_SZ = 100;

    private readonly IMarketDataProvider _marketDataProvider = marketDataProvider;
    private readonly IChannelManager _channelManager = channelManager;
    private readonly IBarRepository _barRepository = barRepository;
    private readonly ILogger<MarketDataIngestion> _logger = logger;

    // TODO: save bars to Parquet files AND Redis cache. do not allow client to receive raw socket stream data

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        await Task.WhenAll(
            ProcessMarketDataContextsAsync(cancellationToken),
            ProcessMarketNewsContextsAsync(cancellationToken)
        );
    }

    private async Task ProcessMarketDataContextsAsync(CancellationToken cancellationToken)
    {
        _logger.LogDebug("Processing market data...");

        await foreach (MarketDataContext context in _channelManager.MarketDataContextChannel.Reader.ReadAllAsync(cancellationToken))
        {
            _logger.LogDebug("Processing symbols: {symbols}", string.Join(',', context.Request.Symbols));

            List<Bar> barsToWrite = [];

            await foreach (Bar bar in _marketDataProvider.StreamBarsAsync(context.Request.Symbols, cancellationToken))
            {
                if (context.MarketDataResponseChannel is not null)
                {
                    try
                    {
                        await context.MarketDataResponseChannel.Writer.WriteAsync(new MarketDataSnapshot
                        {
                            Symbol = bar.Symbol,
                            TimestampUtc = bar.DateTimeUtc,
                            Open = bar.Open,
                            High = bar.High,
                            Low = bar.Low,
                            Close = bar.Close,
                            Volume = bar.Volume,
                        }, cancellationToken);
                    }
                    catch (OperationCanceledException ex)
                    {
                        _logger.LogError(ex.Message);
                    }
                }

                barsToWrite.Add(bar);

                if (barsToWrite.Count >= LIST_BATCH_SZ)
                {
                    await _barRepository.AddRangeAsync(barsToWrite, cancellationToken);
                    barsToWrite.Clear();
                }

                // TODO: redis cache implementation here
            }
        }
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
