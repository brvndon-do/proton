namespace Proton.Engine.Core.Models.Trading;

public class TradeOrder
{
    public required string Symbol { get; set; }
    public OrderSide Side { get; set; }
    public decimal Quantity { get; set; }
    public OrderType OrderType { get; set; }
    public TimeInForce TimeInForce { get; set; }
    public decimal? LimitPrice { get; set; }
    public decimal? StopPrice { get; set; }
    public string? ClientOrderId { get; set; }
}
