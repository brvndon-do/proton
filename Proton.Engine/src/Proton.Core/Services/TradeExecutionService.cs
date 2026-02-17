using Proton.Engine.Core.Models.Trading;
using Proton.Engine.Core.Models.Execution;
using Proton.Engine.Core.Interfaces;

namespace Proton.Engine.Core.Services;

public class TradeExecutionService(IBroker broker)
{
    private readonly IBroker _broker = broker;

    public Task<OrderResult> SubmitOrderAsync(TradeOrder order, CancellationToken cancellationToken = default) => _broker.CreateOrderAsync(order, cancellationToken);

    public async Task<ExecutionBatchResult> SubmitOrdersAsync(IEnumerable<TradeOrder> orders, ExecutionOptions? options = null, CancellationToken cancellationToken = default)
    {
        options ??= new ExecutionOptions();
        List<TradeOrder> orderList = [.. orders];

        SemaphoreSlim semaphore = new SemaphoreSlim(options.MaxDegreeOfParallelism);
        List<Task> tasks = [];
        OrderResult?[] results = new OrderResult?[orderList.Count];
        List<ExecutionFailure> failures = [];

        for (int index = 0; index < orderList.Count; index++)
        {
            TradeOrder order = orderList[index];
            await semaphore.WaitAsync(cancellationToken);

            Task task = Task.Run(async () =>
            {
                try
                {
                    OrderResult result = await _broker.CreateOrderAsync(order, cancellationToken);
                    results[index] = result;
                }
                catch (Exception ex)
                {
                    lock (failures)
                    {
                        failures.Add(new ExecutionFailure
                        {
                            Index = index,
                            Symbol = order.Symbol,
                            ClientOrderId = order.ClientOrderId,
                            Error = ex.Message
                        });
                    }

                    if (!options.ContinueOnError)
                        throw;
                }
                finally
                {
                    semaphore.Release();
                }
            }, cancellationToken);

            tasks.Add(task);
        }

        await Task.WhenAll(tasks);
        List<OrderResult> completed = [.. results.Where(x => x is not null).Select(x => x!)];

        return new ExecutionBatchResult(completed, failures);
    }

    public async Task<bool> CancelOrderAsync(string orderId, CancellationToken cancellationToken = default)
    {
        bool success = await _broker.CancelOrderAsync(orderId, cancellationToken);

        return success;
    }
}
