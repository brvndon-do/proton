namespace Proton.Engine.Core.Models.MarketData;

public class MarketDataSnapshot
{
    public required string Symbol { get; set; }
    public DateTime TimestampUtc { get; set; }
    public decimal Open { get; set; }
    public decimal High { get; set; }
    public decimal Low { get; set; }
    public decimal Close { get; set; }
    public decimal Volume { get; set; }

    public IDictionary<IndicatorType, decimal>? Indicators { get; set; }
}
