namespace Proton.Engine.Core.Models;

public class Bar
{
    public required string Symbol { get; set; }
    public decimal Open { get; set; }
    public decimal High { get; set; }
    public decimal Low { get; set; }
    public decimal Close { get; set; }
    public decimal Volume { get; set; }
    public decimal? Vwap { get; set; }
    public ulong? TradeCount { get; set; }
    public DateTimeOffset DateTimeUtc { get; set; }
}
