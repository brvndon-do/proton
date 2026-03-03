namespace Proton.Engine.Core.Models;

public class Bar
{
    public string Symbol { get; set; } = string.Empty;
    public decimal Open { get; set; }
    public decimal High { get; set; }
    public decimal Low { get; set; }
    public decimal Close { get; set; }
    public decimal Volume { get; set; }
    public decimal? Vwap { get; set; }
    public ulong? TradeCount { get; set; }
    public DateTime DateTimeUtc { get; set; }
}
