using System.Threading.Channels;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Proton.Engine.AppHost.Grpc;
using Proton.Engine.AppHost.Utilities;
using Proton.Engine.Core.Interfaces;
using Proton.Engine.Core.Interfaces.Repositories;
using Proton.Engine.Core.Models;
using Proton.Engine.Core.Models.MarketData;

namespace Proton.Engine.AppHost.Services.Grpc;

public class MarketDataService(
    IMarketDataSubscriptionManager marketDataSubscriptionManager,
    ICacheRepository cacheRepository,
    IIndicatorService indicatorService,
    ILogger<MarketDataService> logger
) : MarketData.MarketDataBase
{
    private readonly IMarketDataSubscriptionManager _marketDataSubscriptionManager = marketDataSubscriptionManager;
    private readonly ICacheRepository _cacheRepository = cacheRepository;
    private readonly IIndicatorService _indicatorService = indicatorService;
    private readonly ILogger<MarketDataService> _logger = logger;

    public override async Task StreamMarketSnapshot(MarketSnapshotRequest request, IServerStreamWriter<MarketSnapshot> responseStream, ServerCallContext context)
    {
        CancellationToken cancellationToken = context.CancellationToken;
        List<IndicatorType> requestedIndicators = [.. request.Indicators.Select(x => System.Enum.Parse<IndicatorType>(x))];

        foreach (string symbol in request.Symbols)
        {
            _logger.LogInformation($"Subscribing to {symbol}");

            Channel<Bar> subscriptionChannel = await _marketDataSubscriptionManager.SubscribeAsync(symbol, cancellationToken: cancellationToken);
            int windowSize = requestedIndicators.Any()
                ? _indicatorService.IndicatorWindowValues
                    .Where(x => requestedIndicators.Contains(x.Key))
                    .Max(x => x.Value)
                : 0;
            List<Bar> bars = [.. await _cacheRepository.GetLatestBarsAsync(symbol, windowSize, cancellationToken)];

            try
            {
                await foreach (Bar bar in subscriptionChannel.Reader.ReadAllAsync(cancellationToken))
                {
                    MarketDataSnapshot snapshot = BuildMarketDataSnapshot(bar, bars, requestedIndicators);
                    await responseStream.WriteAsync(snapshot.ToGrpc(), cancellationToken);
                }
            }
            finally
            {
                _logger.LogInformation($"Unsubscribed from {symbol}");
                await _marketDataSubscriptionManager.UnsubscribeAsync(symbol, cancellationToken);
            }
        }
    }

    // TODO: probably read from redis cache as well instead of directly streaming to client
    public override async Task StreamNewsSnapshot(Empty request, IServerStreamWriter<NewsSnapshot> responseStream, ServerCallContext context) => throw new NotImplementedException();

    public override async Task GetNewsSnapshot(NewsSnapshotRequest request, IServerStreamWriter<NewsSnapshot> responseStream, ServerCallContext context) => throw new NotImplementedException();

    private MarketDataSnapshot BuildMarketDataSnapshot(Bar bar, List<Bar> bars, List<IndicatorType> requestedIndicators)
    {
        Dictionary<IndicatorType, decimal> indicators = [];

        foreach (IndicatorType type in requestedIndicators)
        {
            IEnumerable<decimal> values = _indicatorService.CalculateIndicator(type, bars);
            indicators[type] = values.LastOrDefault();
        }

        return new MarketDataSnapshot
        {
            Symbol = bar.Symbol,
            TimestampUtc = bar.DateTimeUtc,
            Open = bar.Open,
            High = bar.High,
            Low = bar.Low,
            Close = bar.Close,
            Volume = bar.Volume,
            Indicators = indicators
        };
    }
}
