namespace Proton.Engine.MarketDataIngestion;

public sealed class MarketDataIngestionOptions
{
    public const string SectionName = nameof(MarketDataIngestionOptions);

    public string? BackfillTimeFrame { get; set; }
    public int? BackfillLookbackDays { get; set; }
}
