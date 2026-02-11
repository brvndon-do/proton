using Proton.Engine.Core.Models;
using Proton.Engine.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace Proton.Engine.Core.Services;

public class TradingService(IBroker broker, ILogger<TradingService> logger)
{
    private readonly IBroker _broker = broker;
    private readonly ILogger<TradingService> _logger = logger;

    public async Task<IEnumerable<OrderResult>> CreateOrdersAsync(IEnumerable<TradeOrder> orders, int maxDegreeOfParallelism = 50, CancellationToken cancellationToken = default)
    {
        _logger.LogTrace("Fulfilling {count} orders", orders.Count());

        List<OrderResult> results = new List<OrderResult>();
        SemaphoreSlim semaphore = new SemaphoreSlim(maxDegreeOfParallelism);
        List<Task> tasks = [];

        foreach (TradeOrder order in orders)
        {
            await semaphore.WaitAsync();
            Task task = Task.Run(async () =>
            {
                try
                {
                    OrderResult result = await _broker.CreateOrderAsync(order, cancellationToken);
                    lock (results)
                    {
                        results.Add(result);
                    }
                }
                finally
                {
                    semaphore.Release();
                }
            });

            tasks.Add(task);
        }

        await Task.WhenAll(tasks);
        return results;
    }

    public async Task<bool> CancelOrderAsync(string orderId, CancellationToken cancellationToken = default)
    {
        bool success = await _broker.CancelOrderAsync(orderId, cancellationToken);
        _logger.LogTrace("[{success}]: cancel orderId {orderId}", success ? "SUCCESS" : "FAILURE", orderId);

        return success;
    }

    public async Task CancelOrdersAsync(CancellationToken cancellationToken = default)
    {

    }
}
