namespace Proton.Engine.Core.Models.MarketData;

public sealed class MarketDataSnapshot
{
    public required string Symbol { get; init; }
    public DateTime TimestampUtc { get; init; }
    public decimal Open { get; init; }
    public decimal High { get; init; }
    public decimal Low { get; init; }
    public decimal Close { get; init; }
    public decimal Volume { get; init; }

    public IDictionary<IndicatorType, decimal>? Indicators { get; init; }
}
