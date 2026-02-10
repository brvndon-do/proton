namespace Proton.Engine.Core.Models;

public enum OrderSide
{
    Buy = 0,
    Sell
}

public enum OrderType
{
    Market = 0,
    Limit,
    Stop,
    StopLimit
}

public enum TimeInForce
{
    Day = 0,
    Gtc,
    Ioc,
    Fok
}

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
