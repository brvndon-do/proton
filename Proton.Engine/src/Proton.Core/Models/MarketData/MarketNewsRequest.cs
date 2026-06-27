namespace Proton.Engine.Core.Models.MarketData;

public sealed class MarketNewsRequest
{
    public required IEnumerable<string> Symbols { get; init; }

    // TODO: rename to just "start" and "end"
    public DateTime? StartInterval { get; init; }
    public DateTime? EndInterval { get; init; }
    public int Limit { get; init; } = 10;
}
