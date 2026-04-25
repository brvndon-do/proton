using System.Runtime.CompilerServices;
using Proton.Engine.Core.Interfaces;
using Proton.Engine.Core.Models;
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

    private static readonly Random _random = new();
    private readonly HashSet<string> _subscribedSymbols = [];

    public Task ConnectAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task DisconnectAsync(CancellationToken cancellationToken = default)
    {
        _subscribedSymbols.Clear();
        return Task.CompletedTask;
    }

    public Task SubscribeToSymbolAsync(string symbol, CancellationToken cancellationToken = default)
    {
        _subscribedSymbols.Add(symbol);
        return Task.CompletedTask;
    }

    public Task UnsubscribeToSymbolAsync(string symbol, CancellationToken cancellationToken = default)
    {
        _subscribedSymbols.Remove(symbol);
        return Task.CompletedTask;
    }

    public async IAsyncEnumerable<Bar> StreamBarsAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            foreach (string symbol in _subscribedSymbols)
                yield return GenerateBar(symbol);

            await Task.Delay(_random.Next(300, 1500), cancellationToken);
        }
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
            DateTime current = start;
            int count = 0;
            while (current <= end && count < limit)
            {
                bars.Add(GenerateBar(symbol, current));
                current = current.AddMinutes(stepMinutes);
                count++;
            }
        }

        return Task.FromResult<IEnumerable<Bar>>(bars);
    }

    private static Bar GenerateBar(string symbol, DateTime? dateTimeUtc = null)
    {
        if (!TickerRanges.TryGetValue(symbol, out var range))
            range = (100m, 200m, 100000, 500000);

        decimal open = RandomDecimal(range.min, range.max);
        decimal close = RandomDecimal(range.min, range.max);
        decimal high = Math.Max(open, close) + RandomDecimal(0, 5);
        decimal low = Math.Min(open, close) - RandomDecimal(0, 5);
        long volume = RandomLong(range.minVol, range.maxVol);
        decimal vwap = (open + close + high + low) / 4;
        int tradeCount = _random.Next(100, 1000);

        return new Bar
        {
            Symbol = symbol,
            Open = open,
            High = high,
            Low = low,
            Close = close,
            Volume = volume,
            Vwap = vwap,
            TradeCount = (ulong)tradeCount,
            DateTimeUtc = dateTimeUtc ?? DateTime.UtcNow,
        };
    }

    private static decimal RandomDecimal(decimal min, decimal max) => min + (decimal)_random.NextDouble() * (max - min);

    private static long RandomLong(long min, long max) => min + (long)(_random.NextDouble() * (max - min));
}
