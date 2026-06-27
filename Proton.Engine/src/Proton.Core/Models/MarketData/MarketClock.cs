namespace Proton.Engine.Core.Models.MarketData;

public sealed class MarketClock
{
    public bool IsOpen { get; init; }
    public DateTime TimestampUtc { get; init; }
    public DateTime NextOpenUtc { get; init; }
    public DateTime NextCloseUtc { get; init; }
}
