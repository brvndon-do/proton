using Proton.Engine.Core.Models.Trading;

namespace Proton.Engine.TradeExecution.Models;

public sealed class ExecutionBatchResult(IReadOnlyList<OrderResult> results, IReadOnlyList<ExecutionFailure> failures)
{
    public IReadOnlyList<OrderResult> Results { get; } = results;
    public IReadOnlyList<ExecutionFailure> Failures { get; } = failures;
    public bool HasFailures => Failures.Count > 0;
}
