namespace Proton.Engine.Core.Models.MarketData;

public sealed class MarketNewsSnapshot
{
    public required string Headline { get; init; }
    public string? Summary { get; init; }
    public string? Source { get; init; }
    public IEnumerable<string>? Symbols { get; init; }
    public DateTime CreatedAtUtc { get; init; }
}
