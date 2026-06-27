namespace Proton.Engine.TradeExecution.Models;

public sealed class ExecutionOptions
{
    public int MaxDegreeOfParallelism { get; init; } = 10;
    public bool ContinueOnError { get; init; } = true;
}
