namespace Proton.Engine.Core.Models.MarketData;

public enum Indicators
{

}

public class MarketDataSnapshot
{
    public required string Symbol { get; set; }
    public DateTimeOffset TimestampUtc { get; set; }
    public decimal Open { get; set; }
    public decimal High { get; set; }
    public decimal Low { get; set; }
    public decimal Close { get; set; }
    public decimal Volume { get; set; }

    // TODO: DON'T FORGET TO PROPERLY TYPE THE KEY INSTEAD OF USING A STRING...
    public required IDictionary<string, decimal> Indicators { get; set; }
}
