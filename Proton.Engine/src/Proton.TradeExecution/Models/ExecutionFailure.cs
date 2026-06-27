namespace Proton.Engine.TradeExecution.Models;

public sealed class ExecutionFailure
{
    public int Index { get; init; }
    public string? Symbol { get; init; }
    public string? ClientOrderId { get; init; }
    public string Error { get; init; } = string.Empty;
}
