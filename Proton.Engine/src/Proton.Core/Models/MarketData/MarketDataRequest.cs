namespace Proton.Engine.Core.Models.MarketData;

public class MarketDataRequest
{
    public required IEnumerable<string> Symbols { get; set; }

    // TODO: USE AN ENUM?
    public required string Timeframe { get; set; }
}
