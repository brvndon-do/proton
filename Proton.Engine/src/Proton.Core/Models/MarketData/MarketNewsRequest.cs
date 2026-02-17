namespace Proton.Engine.Core.Models.MarketData;

public class MarketNewsRequest
{
    public required IEnumerable<string> Symbols { get; set; }
    public DateTimeOffset? StartInterval { get; set; }
    public DateTimeOffset? EndInterval { get; set; }
    public int Limit { get; set; } = 10;
}
