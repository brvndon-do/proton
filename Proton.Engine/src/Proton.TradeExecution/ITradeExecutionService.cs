using Proton.Engine.Core.Models.Trading;
using Proton.Engine.TradeExecution.Models;

namespace Proton.Engine.TradeExecution;

public interface ITradeExecutionService
{
    Task<OrderResult> SubmitOrderAsync(TradeOrder order, CancellationToken cancellationToken = default);
    Task<ExecutionBatchResult> SubmitOrdersAsync(IReadOnlyList<TradeOrder> orders, ExecutionOptions? options = null, CancellationToken cancellationToken = default);
    Task<bool> CancelOrderAsync(string orderId, CancellationToken cancellationToken = default);
}
