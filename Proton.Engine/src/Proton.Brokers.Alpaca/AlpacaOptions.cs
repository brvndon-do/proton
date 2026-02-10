namespace Proton.Engine.Brokers.Alpaca;

public sealed class AlpacaOptions
{
    public static string SectionName = "AlpacaOptions";

    public required string ApiKey { get; set; }
    public required string ApiSecret { get; set; }
    public required bool IsPaperAccount { get; set; }
}
