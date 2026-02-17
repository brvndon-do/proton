namespace Proton.Engine.Core.Models.MarketData;

public class MarketNewsSnapshot
{
    public required string Headline { get; set; }
    public string? Summary { get; set; }
    public string? Source { get; set; }
    public IEnumerable<string>? Symbols { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
}
