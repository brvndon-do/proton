using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Proton.Engine.Core.Interfaces;
using Proton.Engine.Core.Models;
using Proton.Engine.Core.Models.MarketData;

namespace Proton.Engine.MarketDataIngestion;

public class MarketDataIngestion(
    IMarketDataProvider marketDataProvider,
    IChannelManager channelManager,
    IIndicatorService indicatorService,
    ILogger<MarketDataIngestion> logger
) : BackgroundService
{
    private readonly IMarketDataProvider _marketDataProvider = marketDataProvider;
    private readonly IChannelManager _channelManager = channelManager;
    private readonly IIndicatorService _indicatorService = indicatorService;
    private readonly ILogger<MarketDataIngestion> _logger = logger;

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        _ = Task.Run(async () =>
        {
            await foreach (MarketDataContext context in _channelManager.MarketDataContextChannel.Reader.ReadAllAsync())
            {
                int maxWindow = context.Request.Indicators.Max(x => _indicatorService.IndicatorWindowValues[x]);

                _ = Task.Run(async () =>
                {
                    Dictionary<string, Queue<Bar>> symbolsMap = [];

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
