using Microsoft.Extensions.Logging;
using Proton.Engine.Core.Interfaces;
using Proton.Engine.Core.Models.Trading;
using Proton.Engine.TradeExecution.Models;

namespace Proton.Engine.TradeExecution;

public class TradeExecutionService(IOrderGateway orderGateway, ILogger<TradeExecutionService> logger) : ITradeExecutionService
{
    private readonly IOrderGateway _orderGateway = orderGateway;
    private readonly ILogger<TradeExecutionService> _logger = logger;

    public Task<OrderResult> SubmitOrderAsync(TradeOrder order, CancellationToken cancellationToken = default) => _orderGateway.CreateOrderAsync(order, cancellationToken);

    public async Task<ExecutionBatchResult> SubmitOrdersAsync(IReadOnlyList<TradeOrder> orders, ExecutionOptions? options = null, CancellationToken cancellationToken = default)
    {
        options ??= new ExecutionOptions();
        SemaphoreSlim semaphore = new SemaphoreSlim(options.MaxDegreeOfParallelism);
        List<Task> tasks = [];
        List<ExecutionFailure> failures = [];
        OrderResult?[] results = new OrderResult?[orders.Count];

        for (int i = 0; i < orders.Count; i++)
        {
            TradeOrder order = orders[i];
            await semaphore.WaitAsync(cancellationToken);

            Task task = Task.Run(async () =>
            {
                try
                {
                    OrderResult result = await _orderGateway.CreateOrderAsync(order, cancellationToken);
                    results[i] = result;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to submit order");

                    lock (failures)
                    {
                        failures.Add(new ExecutionFailure
                        {
                            Index = i,
                            Symbol = order.Symbol,
                            ClientOrderId = order.ClientOrderId,
                            Error = ex.Message,
                        });
                    }

                    if (!options.ContinueOnError)
                        throw;
                }
                finally
                {
                    semaphore.Release();
                }
            });

            tasks.Add(task);
        }

        await Task.WhenAll(tasks);
        List<OrderResult> completed = [.. results.Where(x => x is not null).Select(x => x!)];

        return new ExecutionBatchResult(completed, failures);
    }

    public Task<bool> CancelOrderAsync(string orderId, CancellationToken cancellationToken = default) => _orderGateway.CancelOrderAsync(orderId, cancellationToken);
}
