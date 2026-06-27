namespace Proton.Engine.Core.Models;

public sealed class Bar
{
    public string Symbol { get; init; } = string.Empty;
    public decimal Open { get; init; }
    public decimal High { get; init; }
    public decimal Low { get; init; }
    public decimal Close { get; init; }
    public decimal Volume { get; init; }
    public decimal? Vwap { get; init; }
    public ulong? TradeCount { get; init; }
    public DateTime DateTimeUtc { get; init; }
}
