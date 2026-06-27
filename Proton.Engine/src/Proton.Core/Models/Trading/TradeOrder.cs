namespace Proton.Engine.Core.Models.Trading;

public sealed class TradeOrder
{
    public required string Symbol { get; init; }
    public OrderSide Side { get; init; }
    public decimal Quantity { get; init; }
    public OrderType OrderType { get; init; }
    public TimeInForce TimeInForce { get; init; }
    public decimal? LimitPrice { get; init; }
    public decimal? StopPrice { get; init; }
    public string? ClientOrderId { get; init; }
}
