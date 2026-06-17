using System.Collections.Concurrent;
using System.Threading.Channels;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Proton.Engine.Core.Interfaces;
using Proton.Engine.Core.Interfaces.Repositories;
using Proton.Engine.Core.Models;
using Proton.Engine.Core.Models.Trading;
using Proton.Engine.Core.Utilities;
using Proton.Engine.MarketDataIngestion.Models;

namespace Proton.Engine.MarketDataIngestion;

public class MarketDataSubscriptionManager(
    IMarketDataProvider marketDataProvider,
    ICacheRepository cacheRepository,
    IBarRepository barRepository,
    IOptions<MarketDataIngestionOptions> options,
    ILogger<MarketDataSubscriptionManager> logger
) : IMarketDataSubscriptionManager, IAsyncDisposable
{
    private readonly IMarketDataProvider _marketDataProvider = marketDataProvider;
    private readonly ICacheRepository _cacheRepository = cacheRepository;
    private readonly IBarRepository _barRepository = barRepository;
    private readonly ILogger<MarketDataSubscriptionManager> _logger = logger;

    private readonly MarketDataIngestionOptions _options = options.Value;
    private readonly CancellationTokenSource _cts = new CancellationTokenSource();
    private readonly ConcurrentDictionary<string, SymbolSubscription> _activeSubscriptions = [];

    private readonly Lock _upstreamTaskLock = new();
    private Task? _upstreamTask;

    public async Task PinAsync(string symbol, CancellationToken cancellationToken = default)
    {
        SymbolSubscription subscription = await GetOrCreateSubscriptionAsync(symbol, cancellationToken);
        Interlocked.Increment(ref subscription.PinCount);
    }

    public async Task UnpinAsync(string symbol, CancellationToken cancellationToken = default)
    {
        if (!_activeSubscriptions.TryGetValue(symbol, out SymbolSubscription? subscription) || subscription is null)
        {
            _logger.LogInformation("{symbol} has already been unpinned", symbol);
            return;
        }

        _ = Interlocked.Decrement(ref subscription.PinCount);

        await ReleaseSubscriptionAsync(subscription, cancellationToken);
    }

    // TODO: this returns only a channel for market data subscription, even though it is supposed to consume news related to the symbol as well.
    //       maybe i'm thinking of returning a tuple of channels, one for market data and one for news. but in the future, will other providers
    //       have news? need to think about that as well..
    public async Task<Channel<Bar>> SubscribeAsync(string symbol, int subscriberCapacity = 1_000, CancellationToken cancellationToken = default)
    {
        Channel<Bar> subscriberChannel = Channel.CreateBounded<Bar>(new BoundedChannelOptions(subscriberCapacity)
        {
            FullMode = BoundedChannelFullMode.DropOldest,
        });

        SymbolSubscription subscription = await GetOrCreateSubscriptionAsync(symbol, cancellationToken);

        subscription.SubscriberChannels.Add(subscriberChannel);
        Interlocked.Increment(ref subscription.ActiveSubscribersCount);

        return subscriberChannel;
    }

    public async Task UnsubscribeAsync(string symbol, CancellationToken cancellationToken = default)
    {
        if (!_activeSubscriptions.TryGetValue(symbol, out SymbolSubscription? subscription) || subscription is null)
        {
            _logger.LogInformation("{symbol} has already been unsubscribed", symbol);
            return;
        }

        _ = Interlocked.Decrement(ref subscription.ActiveSubscribersCount);

        await ReleaseSubscriptionAsync(subscription, cancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        await _cts.CancelAsync();

        if (_upstreamTask is not null)
            await _upstreamTask;

        _cts.Dispose();
    }

    private async Task<SymbolSubscription> GetOrCreateSubscriptionAsync(string symbol, CancellationToken cancellationToken)
    {
        SymbolSubscription subscription = _activeSubscriptions.GetOrAdd(symbol, key =>
        {
            SymbolSubscription created = new()
            {
                Symbol = key,
                SubscriberChannels = [],
                StartedAtUtc = DateTime.UtcNow,
            };

            // Lazy<T> will ensure BackfillIfNeededAsync and SubscribeToSymbolAsync runs once per subscriber; prevents data races
            created.Initialization = new Lazy<Task>(async () =>
            {
                try
                {
                    await BackfillIfNeededAsync(created, _cts.Token);
                    await _marketDataProvider.SubscribeToSymbolAsync(created.Symbol, _cts.Token);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to backfill/subscribe to symbol to {Symbol}", symbol);
                    _ = _activeSubscriptions.TryRemove(symbol, out _);
                    throw;
                }
            });

            return created;
        });

        await subscription.Initialization.Value.WaitAsync(cancellationToken);

        EnsureUpstreamTaskRunning();

        return subscription;
    }

    // NOTE: another potential race condition -- if two threads that unsubscribes/unpins, they can both enter the teardown method
    //       the solution is idempotency or locking the symbol, but this is low pri right now. 
    private async Task ReleaseSubscriptionAsync(SymbolSubscription subscription, CancellationToken cancellationToken)
    {
        if (subscription.ActiveSubscribersCount <= 0 && subscription.PinCount <= 0)
        {
            _ = _activeSubscriptions.TryRemove(subscription.Symbol, out _);
            await _marketDataProvider.UnsubscribeToSymbolAsync(subscription.Symbol, cancellationToken);
        }

        if (_activeSubscriptions.IsEmpty)
            await _marketDataProvider.DisconnectAsync(cancellationToken);
    }

    private void EnsureUpstreamTaskRunning()
    {
        if (_upstreamTask is not null)
            return;

        lock (_upstreamTaskLock)
        {
            _upstreamTask ??= Task.Run(() => StartMarketDataUpstream(_cts.Token));
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
                    await _barRepository.AddRangeAsync(buffer, CancellationToken.None);
            }

            foreach (SymbolSubscription subscription in _activeSubscriptions.Values)
            {
                foreach (Channel<Bar> channel in subscription.SubscriberChannels)
                    channel.Writer.TryComplete();
            }

            _activeSubscriptions.Clear();
        }
    }

    private async Task BackfillIfNeededAsync(SymbolSubscription subscription, CancellationToken cancellationToken = default)
    {
        string symbol = subscription.Symbol;
        // TODO: maybe add overload for x limit of bars from a file read..
        List<Bar> bars = [.. await _barRepository.ReadBarsAsync(symbol, cancellationToken)];

        if (!bars.Any())
        {
            List<Bar> historicalBars = [.. await _marketDataProvider.GetHistoricalBarsAsync(
                symbol,
                timeFrame: _options.BackfillTimeFrame?.ConvertToTimeFrame() ?? TimeFrame.Daily,
                from: DateTime.UtcNow.AddDays(-(_options.BackfillLookbackDays ?? 30)),  // RANGE:
                to: DateTime.UtcNow,                                                    // 30 DAYS
                cancellationToken: cancellationToken
            )];

            bars.AddRange(historicalBars);
            await _barRepository.AddRangeAsync(historicalBars, cancellationToken);
        }

        await _cacheRepository.AddRangeAsync(bars, cancellationToken);
    }
}
