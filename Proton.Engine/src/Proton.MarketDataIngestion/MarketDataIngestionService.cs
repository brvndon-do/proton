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
    IIndicatorService indicatorService,
    IBarRepository barRepository,
    ILogger<MarketDataIngestion> logger
) : BackgroundService
{
    private const int LIST_BATCH_SZ = 10;

    private readonly IMarketDataProvider _marketDataProvider = marketDataProvider;
    private readonly IChannelManager _channelManager = channelManager;
    private readonly IIndicatorService _indicatorService = indicatorService;
    private readonly IBarRepository _barRepository = barRepository;
    private readonly ILogger<MarketDataIngestion> _logger = logger;

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

            int maxWindow = context.Request.Indicators.Max(x => _indicatorService.IndicatorWindowValues[x]);

            _ = Task.Run(async () =>
            {
                Dictionary<string, Queue<Bar>> symbolsMap = [];
                List<Bar> barsToWrite = [];

                await foreach (Bar bar in _marketDataProvider.StreamBarsAsync(context.Request.Symbols, cancellationToken))
                {
                    string symbol = bar.Symbol;

                    if (!symbolsMap.ContainsKey(symbol))
                        symbolsMap[symbol] = new Queue<Bar>(maxWindow);

                    Queue<Bar> buffer = symbolsMap[symbol];
                    if (buffer.Count >= maxWindow)
                        buffer.Dequeue();

                    buffer.Enqueue(bar);

                    bool allReady = context.Request.Indicators.All(x => buffer.Count >= _indicatorService.IndicatorWindowValues[x]);
                    if (allReady)
                    {
                        Dictionary<IndicatorType, decimal> indicators = context.Request.Indicators
                            .ToDictionary(
                                x => x,
                                x =>
                                {
                                    int window = _indicatorService.IndicatorWindowValues[x];
                                    IEnumerable<Bar> windowBars = buffer.Skip(buffer.Count - window).Take(window);

                                    return _indicatorService.CalculateIndicator(x, windowBars).Last();
                                }
                            );

                        await context.MarketDataResponseChannel.Writer.WriteAsync(new MarketDataSnapshot
                        {
                            Symbol = bar.Symbol,
                            TimestampUtc = bar.DateTimeUtc,
                            Open = bar.Open,
                            High = bar.High,
                            Low = bar.Low,
                            Close = bar.Close,
                            Volume = bar.Volume,
                            Indicators = indicators,
                        }, cancellationToken);
                    }

                    barsToWrite.Add(bar);

                    if (barsToWrite.Count >= LIST_BATCH_SZ)
                    {
                        await _barRepository.AddRangeAsync(barsToWrite);
                        barsToWrite.Clear();
                    }
                }
            }, cancellationToken);
        }
    }

    private async Task ProcessMarketNewsContextsAsync(CancellationToken cancellationToken)
    {
        _logger.LogDebug("Processing news data...");

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
            }, cancellationToken);
        }
    }
}
