namespace Proton.Engine.Core.Models;

public enum OrderState
{
    Unknown = 0,
    New,
    PartiallyFilled,
    Filled,
    Cancelled,
    Rejected
}

public class OrderStatus
{
    public required string OrderId { get; set; }
    public OrderState State { get; set; }
    public decimal FilledQuantity { get; set; }
    public decimal RemainingQuantity { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public string? Reason { get; set; }
}
