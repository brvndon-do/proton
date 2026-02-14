namespace Proton.Engine.Core.Models.Trading;

public class OrderResult
{
    public required string OrderId { get; set; }
    public OrderStatus? Status { get; set; }
    public DateTimeOffset SubmittedAt { get; set; }
    public required string Symbol { get; set; }
    public OrderSide Side { get; set; }
    public decimal? Quantity { get; set; }
    public decimal? FilledQuantity { get; set; }
    public decimal? AverageFillPrice { get; set; }
    public string? Message { get; set; }
    public IReadOnlyList<string>? Errors { get; set; }
}
