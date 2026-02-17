namespace Proton.Engine.Core.Models.Trading;

public class OrderStatus
{
    public required string OrderId { get; set; }
    public OrderState State { get; set; }
    public decimal FilledQuantity { get; set; }
    public decimal RemainingQuantity { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
    public string? Reason { get; set; }
}
