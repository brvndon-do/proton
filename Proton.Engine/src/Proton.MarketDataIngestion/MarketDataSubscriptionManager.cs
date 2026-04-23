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

    private readonly Lock _upstreamTaskLock = new();
    private Task? _upstreamTask;

    // TODO: this returns only a channel for market data subscription, even though it is supposed to consume news related to the symbol as well.
    //       maybe i'm thinking of returning a tuple of channels, one for market data and one for news. but in the future, will other providers
    //       have news? need to think about that as well..
    public async Task<Channel<Bar>> SubscribeAsync(string symbol, int subscriberCapacity = 1_000, CancellationToken cancellationToken = default)
    {
        Channel<Bar> subscriberChannel = Channel.CreateBounded<Bar>(new BoundedChannelOptions(subscriberCapacity)
        {
            FullMode = BoundedChannelFullMode.DropOldest,
        });

        bool isNewSubscription = false;
        SymbolSubscription subscription = _activeSubscriptions.GetOrAdd(symbol, _ =>
        {
            isNewSubscription = true;

            return new SymbolSubscription
            {
                Symbol = symbol,
                SubscriberChannels = [],
                StartedAtUtc = DateTime.UtcNow,
            };
        });

        subscription.SubscriberChannels.Add(subscriberChannel);
        Interlocked.Increment(ref subscription.ActiveSubscribersCount);

        if (isNewSubscription)
        {
            await BackfillIfNeededAsync(subscription);
            await _marketDataProvider.SubscribeToSymbolAsync(symbol, cancellationToken);
        }

        EnsureUpstreamTaskRunning(cancellationToken);

        return subscriberChannel;
    }

    private void EnsureUpstreamTaskRunning(CancellationToken cancellationToken)
    {
        if (_upstreamTask is not null)
            return;

        lock (_upstreamTaskLock)
        {
            _upstreamTask ??= Task.Run(() => StartMarketDataUpstream(cancellationToken));
        }
    }

    private async Task StartMarketDataUpstream(CancellationToken cancellationToken)
    {
        const int PARQUET_BUFFER_MAX_SZ = 100;

        ConcurrentDictionary<string, List<Bar>> batchBuffers = [];

        _logger.LogInformation("Starting market data upstream..");

        try
        {
            await foreach (Bar bar in _marketDataProvider.StreamBarsAsync(cancellationToken))
            {
                if (!_activeSubscriptions.TryGetValue(bar.Symbol, out SymbolSubscription? subscription))
                    continue;

                await _cacheRepository.AddAsync(bar, cancellationToken);

                List<Bar> buffer = batchBuffers.GetOrAdd(bar.Symbol, (_) => []);
                buffer.Add(bar);

                if (buffer.Count >= PARQUET_BUFFER_MAX_SZ)
                {
                    await _barRepository.AddRangeAsync(buffer, cancellationToken);
                    buffer.Clear();
                }

                foreach (Channel<Bar> channel in subscription.SubscriberChannels)
                    channel.Writer.TryWrite(bar);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Market data upstream cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Market data upstream error");
        }
        finally
        {
            foreach (List<Bar> buffer in batchBuffers.Values)
            {
                if (buffer.Any())
                    await _barRepository.AddRangeAsync(buffer, cancellationToken);
            }

            foreach (SymbolSubscription subscription in _activeSubscriptions.Values)
            {
                foreach (Channel<Bar> channel in subscription.SubscriberChannels)
                    channel.Writer.TryComplete();
            }

            _activeSubscriptions.Clear();
        }
    }

    public async Task UnsubscribeAsync(string symbol, CancellationToken cancellationToken = default)
    {
        if (_activeSubscriptions.TryGetValue(symbol, out SymbolSubscription? subscription) || subscription is null)
        {
            _logger.LogInformation("{symbol} has already been unsubscribed", symbol);
            return;
        }

        int remaining = Interlocked.Decrement(ref subscription!.ActiveSubscribersCount);
        if (remaining <= 0)
        {
            _logger.LogInformation("Last subscriber for {symbol} disconnected, stopping upstream connection", symbol);
            await _marketDataProvider.DisconnectAsync(cancellationToken);
        }

        await _marketDataProvider.UnsubscribeToSymbolAsync(symbol, cancellationToken);
    }

    private async Task BackfillIfNeededAsync(SymbolSubscription subscription)
    {
        // TODO
        _logger.LogInformation($"[SIMULATION]: backfilling for symbol {subscription.Symbol}");
    }
}
