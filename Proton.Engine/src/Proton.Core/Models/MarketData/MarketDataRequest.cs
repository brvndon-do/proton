namespace Proton.Engine.Core.Models.MarketData;

public class MarketDataRequest
{
    public required IEnumerable<string> Symbols { get; set; }
    public IEnumerable<IndicatorType>? Indicators { get; set; }
}
