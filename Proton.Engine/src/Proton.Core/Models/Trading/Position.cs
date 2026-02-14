namespace Proton.Engine.Core.Models.Trading;

public class Position
{
    public required string Symbol { get; set; }
    public decimal Quantity { get; set; }
    public decimal AverageEntryPrice { get; set; }
    public decimal CurrentPrice { get; set; }
    public decimal MarketValue { get; set; }
    public decimal UnrealizedPnl { get; set; }
    public decimal UnrealizedPnlPercent { get; set; }
}
