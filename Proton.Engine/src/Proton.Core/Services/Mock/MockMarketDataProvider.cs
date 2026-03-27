using System.Runtime.CompilerServices;
using Proton.Engine.Core.Interfaces;
using Proton.Engine.Core.Models;
using Proton.Engine.Core.Models.MarketData;
using Proton.Engine.Core.Models.Trading;

namespace Proton.Engine.Core.Services.Mock;

public class MockMarketDataProvider : IMarketDataProvider
{
    private static readonly Dictionary<string, (decimal min, decimal max, long minVol, long maxVol)> TickerRanges = new()
    {
        { "MSFT", (300m, 350m, 1000000, 5000000) },
        { "TSLA", (600m, 900m, 2000000, 10000000) },
        { "AAPL", (150m, 200m, 3000000, 12000000) },
        { "GOOG", (2500m, 3000m, 500000, 2000000) },
        { "AMZN", (3200m, 3700m, 800000, 3000000) },
        { "NVDA", (500m, 800m, 1000000, 4000000) },
        { "META", (250m, 350m, 700000, 2500000) },
    };

    private static readonly string[] Headlines =
    [
        "Earnings Beat Expectations",
        "Stock Surges After Announcement",
        "CEO Releases New Vision",
        "Product Launch Drives Growth",
        "Market Reacts to Global Events",
        "Analysts Upgrade Stock",
        "Dividend Increase Announced"
    ];

    private static readonly string[] Summaries =
    [
        "The company reported higher than expected earnings for the quarter.",
        "Shares rose sharply following the latest product announcement.",
        "Leadership outlined a new strategic direction for the coming year.",
        "Investors responded positively to the new product line.",
        "Global events influenced the market's performance today.",
        "Analysts have upgraded their outlook on the stock.",
        "A dividend increase was announced, pleasing shareholders."
    ];

    private static readonly string[] Authors = ["Jane Doe", "John Smith", "Alex Lee", "Morgan Brown"];
    private static readonly string[] Sources = ["Reuters", "Bloomberg", "CNBC", "Yahoo Finance"];

    private static readonly Random _random = new();

    public Task<IEnumerable<NewsArticle>> GetNewsDataAsync(MarketNewsRequest request, CancellationToken cancellationToken = default)
    {
        int limit = request.Limit > 0 ? request.Limit : 10;
        IEnumerable<NewsArticle> articles = Enumerable.Range(0, limit).Select(i => GenerateNewsArticle(request.Symbols));

        return Task.FromResult(articles);
    }

    public async IAsyncEnumerable<Bar> StreamBarsAsync(IEnumerable<string> symbols, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            foreach (string symbol in symbols)
            {
                if (!TickerRanges.TryGetValue(symbol, out var range))
                {
                    range = (100m, 200m, 100000, 500000); // default range
                }

                decimal open = RandomDecimal(range.min, range.max);
                decimal close = RandomDecimal(range.min, range.max);
                decimal high = Math.Max(open, close) + RandomDecimal(0, 5);
                decimal low = Math.Min(open, close) - RandomDecimal(0, 5);
                long volume = RandomLong(range.minVol, range.maxVol);
                decimal vwap = (open + close + high + low) / 4;
                int tradeCount = _random.Next(100, 1000);

                yield return new Bar
                {
                    Symbol = symbol,
                    Open = open,
                    High = high,
                    Low = low,
                    Close = close,
                    Volume = volume,
                    Vwap = vwap,
                    TradeCount = (ulong)tradeCount,
                    DateTimeUtc = DateTime.UtcNow,
                };
            }
            await Task.Delay(_random.Next(300, 1500), cancellationToken); // simulate real-time
        }
    }

    public async IAsyncEnumerable<NewsArticle> StreamNewsDataAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            yield return GenerateNewsArticle(TickerRanges.Keys);
            await Task.Delay(_random.Next(1000, 3000), cancellationToken); // simulate news interval
        }
    }

    private static NewsArticle GenerateNewsArticle(IEnumerable<string> symbols)
    {
        List<string> symbolList = [.. symbols];
        string symbol = symbolList.Count > 0 ? symbolList[_random.Next(symbolList.Count)] : "MSFT";

        return new NewsArticle
        {
            Id = Guid.NewGuid().ToString(),
            Headline = Headlines[_random.Next(Headlines.Length)],
            Summary = Summaries[_random.Next(Summaries.Length)],
            Content = "This is a mock news article for testing purposes.",
            Author = Authors[_random.Next(Authors.Length)],
            Source = Sources[_random.Next(Sources.Length)],
            Symbols = [symbol],
            CreatedAtUtc = DateTime.UtcNow,
        };
    }

    public Task<IEnumerable<Bar>> GetHistoricalBarsAsync(IEnumerable<string> symbols, TimeFrame timeFrame, DateTime? from, DateTime? to, int limit = 1000, CancellationToken cancellationToken = default)
    {
        List<Bar> bars = [];
        DateTime start = from ?? DateTime.UtcNow.AddDays(-5);
        DateTime end = to ?? DateTime.UtcNow;
        int stepMinutes = timeFrame switch
        {
            TimeFrame.Hourly => 60,
            TimeFrame.Daily => 1440,
            _ => 1440
        };

        foreach (string symbol in symbols)
        {
            if (!TickerRanges.TryGetValue(symbol, out var range))
                range = (100m, 200m, 100000, 500000);

            DateTime current = start;
            int count = 0;
            while (current <= end && count < limit)
            {
                decimal open = RandomDecimal(range.min, range.max);
                decimal close = RandomDecimal(range.min, range.max);
                decimal high = Math.Max(open, close) + RandomDecimal(0, 5);
                decimal low = Math.Min(open, close) - RandomDecimal(0, 5);
                long volume = RandomLong(range.minVol, range.maxVol);
                decimal vwap = (open + close + high + low) / 4;
                int tradeCount = _random.Next(100, 1000);

                bars.Add(new Bar
                {
                    Symbol = symbol,
                    Open = open,
                    High = high,
                    Low = low,
                    Close = close,
                    Volume = volume,
                    Vwap = vwap,
                    TradeCount = (ulong)tradeCount,
                    DateTimeUtc = current,
                });
                current = current.AddMinutes(stepMinutes);
                count++;
            }
        }
        return Task.FromResult<IEnumerable<Bar>>(bars);
    }

    private static decimal RandomDecimal(decimal min, decimal max) => min + (decimal)_random.NextDouble() * (max - min);

    private static long RandomLong(long min, long max) => min + (long)(_random.NextDouble() * (max - min));
}
