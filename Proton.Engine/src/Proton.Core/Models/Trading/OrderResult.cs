namespace Proton.Engine.Core.Models.Trading;

public sealed class OrderResult
{
    public required string OrderId { get; init; }
    public OrderStatus? Status { get; init; }
    public DateTime SubmittedAtUtc { get; init; }
    public required string Symbol { get; init; }
    public OrderSide Side { get; init; }
    public decimal? Quantity { get; init; }
    public decimal? FilledQuantity { get; init; }
    public decimal? AverageFillPrice { get; init; }
    public string? Message { get; init; }
    public IReadOnlyList<string>? Errors { get; init; }
}
