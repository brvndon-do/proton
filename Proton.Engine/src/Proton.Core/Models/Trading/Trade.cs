namespace Proton.Engine.Core.Models.Trading;

public class Trade
{
    public required string TradeId { get; set; }
    public string? OrderId { get; set; }
    public required string Symbol { get; set; }
    public OrderSide Side { get; set; }
    public decimal Quantity { get; set; }
    public decimal Price { get; set; }
    public DateTime ExecutedAtUtc { get; set; }
    public decimal? Fees { get; set; }
}
