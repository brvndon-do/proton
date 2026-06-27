namespace Proton.Engine.Core.Models;

public sealed class NewsArticle
{
    public required string Id { get; init; }
    public required string Headline { get; init; }
    public string? Summary { get; init; }
    public string? Content { get; init; }
    public string? Author { get; init; }
    public string? Source { get; init; }
    public IEnumerable<string>? Symbols { get; init; }
    public DateTime CreatedAtUtc { get; init; }
}
