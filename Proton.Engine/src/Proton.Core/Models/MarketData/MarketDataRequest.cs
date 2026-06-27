namespace Proton.Engine.Core.Models.MarketData;

public sealed class MarketDataRequest
{
    public required IEnumerable<string> Symbols { get; init; }
    public IEnumerable<IndicatorType>? Indicators { get; init; }
}
