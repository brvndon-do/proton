namespace Proton.Engine.Core.Models.MarketData;

public class MarketNewsRequest
{
    public required IEnumerable<string> Symbols { get; set; }

    // TODO: rename to just "start" and "end"
    public DateTime? StartInterval { get; set; }
    public DateTime? EndInterval { get; set; }
    public int Limit { get; set; } = 10;
}
