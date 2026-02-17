namespace Proton.Engine.Core.Models;

public class NewsArticle
{
    public required string Id { get; set; }
    public required string Headline { get; set; }
    public string? Summary { get; set; }
    public string? Content { get; set; }
    public string? Author { get; set; }
    public string? Source { get; set; }
    public IEnumerable<string>? Symbols { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
}
