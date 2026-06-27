namespace Proton.Engine.Core.Models.Trading;

public sealed class Trade
{
    public required string TradeId { get; init; }
    public string? OrderId { get; init; }
    public required string Symbol { get; init; }
    public OrderSide Side { get; init; }
    public decimal Quantity { get; init; }
    public decimal Price { get; init; }
    public DateTime ExecutedAtUtc { get; init; }
    public decimal? Fees { get; init; }
}
