namespace Proton.Engine.Core.Models.Trading;

public sealed class Position
{
    public required string Symbol { get; init; }
    public decimal Quantity { get; init; }
    public decimal AverageEntryPrice { get; init; }
    public decimal CurrentPrice { get; init; }
    public decimal MarketValue { get; init; }
    public decimal UnrealizedPnl { get; init; }
    public decimal UnrealizedPnlPercent { get; init; }
}
