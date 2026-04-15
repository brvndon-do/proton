using System.Collections.Concurrent;
using System.Threading.Channels;
using Microsoft.Extensions.Logging;
using Proton.Engine.Core.Interfaces;
using Proton.Engine.Core.Interfaces.Repositories;
using Proton.Engine.Core.Models;
using Proton.Engine.MarketDataIngestion.Models;

namespace Proton.Engine.MarketDataIngestion;

public class MarketDataSubscriptionManager(
    IMarketDataProvider marketDataProvider,
    ICacheRepository cacheRepository,
    IBarRepository barRepository,
    ILogger<MarketDataSubscriptionManager> logger
) : IMarketDataSubscriptionManager
{
    private readonly IMarketDataProvider _marketDataProvider = marketDataProvider;
    private readonly ICacheRepository _cacheRepository = cacheRepository;
    private readonly IBarRepository _barRepository = barRepository;
    private readonly ILogger<MarketDataSubscriptionManager> _logger = logger;

    private readonly ConcurrentDictionary<string, SymbolSubscription> _activeSubscriptions = [];

    public Task<Channel<Bar>> SubscribeAsync(string symbol, int subscriberCapacity = 1_000, CancellationToken cancellationToken = default)
    {
        Channel<Bar> subscriberChannel = Channel.CreateBounded<Bar>(new BoundedChannelOptions(subscriberCapacity)
        {
            FullMode = BoundedChannelFullMode.DropOldest,
        });

        bool isNewSubscription = false;
        SymbolSubscription subscription = _activeSubscriptions.GetOrAdd(symbol, _ =>
        {
            isNewSubscription = true;
            CancellationTokenSource cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            return new SymbolSubscription
            {
                Symbol = symbol,
                SubscriptionTask = Task.CompletedTask,
                SubscriptionCancellationTokenSource = cancellationTokenSource,
                SubscriberChannels = [],
                StartedAtUtc = DateTime.UtcNow,
            };
        });

        subscription.SubscriberChannels.Add(subscriberChannel);
        Interlocked.Increment(ref subscription.ActiveSubscribersCount);

        if (isNewSubscription)
            subscription.SubscriptionTask = StartUpstreamIngestion(symbol, subscription);

        return Task.FromResult(subscriberChannel);
    }

    public Task UnsubscribeAsync(string symbol, CancellationToken cancellationToken = default)
    {
        if (_activeSubscriptions.TryGetValue(symbol, out SymbolSubscription? subscription) || subscription is null)
        {
            _logger.LogInformation("{symbol} has already been unsubscribed", symbol);
            return Task.CompletedTask;
        }

        int remaining = Interlocked.Decrement(ref subscription!.ActiveSubscribersCount);
        if (remaining <= 0)
        {
            _logger.LogInformation("Last subscriber for {symbol} disconnected, stopping upstream connection", symbol);
            subscription.SubscriptionCancellationTokenSource.Cancel();
        }

        return Task.CompletedTask;
    }

    private async Task StartUpstreamIngestion(string symbol, SymbolSubscription subscription)
    {
        const int PARQUET_BUFFER_MAX_SZ = 100;
        CancellationToken cancellationToken = subscription.SubscriptionCancellationTokenSource.Token;
        List<Bar> batchBuffer = [];

        _logger.LogInformation("Starting upstream ingestion for {symbol}", symbol);

        try
        {
            await BackfillIfNeededAsync(symbol, subscription);

            await foreach (Bar bar in _marketDataProvider.StreamBarsAsync([symbol], cancellationToken))
            {
                await _cacheRepository.AddRangeAsync(batchBuffer, cancellationToken);

                batchBuffer.Add(bar);
                if (batchBuffer.Count >= PARQUET_BUFFER_MAX_SZ)
                {
                    await _barRepository.AddRangeAsync(batchBuffer, cancellationToken);
                    batchBuffer.Clear();
                }

                foreach (Channel<Bar> subscriberChannel in subscription.SubscriberChannels)
                    subscriberChannel.Writer.TryWrite(bar);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred");
        }
        finally
        {
            if (batchBuffer.Any())
                await _barRepository.AddRangeAsync(batchBuffer);

            foreach (Channel<Bar> subscriberChannel in subscription.SubscriberChannels)
                subscriberChannel.Writer.TryComplete();

            _activeSubscriptions.TryRemove(symbol, out _);

            _logger.LogInformation("Upstream connection for {symbol} stopped", symbol);
        }
    }

    private async Task BackfillIfNeededAsync(string symbol, SymbolSubscription subscription)
    {
        // TODO
    }
}
