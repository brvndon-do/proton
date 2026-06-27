namespace Proton.Engine.Core.Models.Trading;

public sealed class OrderStatus
{
    public required string OrderId { get; init; }
    public OrderState State { get; init; }
    public decimal FilledQuantity { get; init; }
    public decimal RemainingQuantity { get; init; }
    public DateTime UpdatedAtUtc { get; init; }
    public string? Reason { get; init; }
}
