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
    IChannelManager channelManager,
    IMarketDataProvider marketDataProvider,
    IMarketDataSubscriptionManager marketDataSubscriptionManager,
    ICacheRepository cacheRepository,
    IIndicatorService indicatorService,
    ILogger<MarketDataService> logger
) : MarketData.MarketDataBase
{
    private readonly IChannelManager _channelManager = channelManager;
    private readonly IMarketDataProvider _marketDataProvider = marketDataProvider;
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
                await _marketDataSubscriptionManager.UnsubscribeAsync(symbol, cancellationToken);
            }
        }
    }

    public override async Task StreamNewsSnapshot(Empty request, IServerStreamWriter<NewsSnapshot> responseStream, ServerCallContext context)
    {
        // TODO: probably read from redis cache as well instead of directly streaming to client

        CancellationToken cancellationToken = context.CancellationToken;
        Channel<MarketNewsSnapshot> responseChannel = Channel.CreateBounded<MarketNewsSnapshot>(100);

        await _channelManager.MarketNewsContextChannel.Writer.WriteAsync(new MarketNewsContext
        {
            MarketNewsResponseChannel = responseChannel,
        }, cancellationToken);

        try
        {
            await foreach (MarketNewsSnapshot snapshot in responseChannel.Reader.ReadAllAsync(cancellationToken))
            {
                await responseStream.WriteAsync(snapshot.ToGrpc(), cancellationToken);
            }
        }
        catch (OperationCanceledException ex)
        {
            _logger.LogInformation(ex.Message); // TODO: better logging
        }
        catch (RpcException ex)
        {
            _logger.LogInformation(ex.Message); // TODO: better logging
        }
    }

    public override async Task GetNewsSnapshot(NewsSnapshotRequest request, IServerStreamWriter<NewsSnapshot> responseStream, ServerCallContext context)
    {
        // TODO: refactor this? don't know if it's an anti-pattern to rely on GetNewsdataAsync() since the other streaming methods
        //       rely on Channel<T> for communication, but we're calling this one straight up.. and we're also just rewriting logic
        //       for mapping Core models to gRPC models explicitly here. RELOOK!!!

        CancellationToken cancellationToken = context.CancellationToken;

        List<NewsArticle> articles = [.. await _marketDataProvider.GetNewsDataAsync(request.ToCore(), cancellationToken)];
        foreach (NewsArticle article in articles)
        {
            NewsSnapshot snapshot = new NewsSnapshot
            {
                Headline = article.Headline,
                Summary = article.Summary,
                Source = article.Source,
                CreatedAt = Timestamp.FromDateTime(article.CreatedAtUtc)
            };
            snapshot.Symbols.AddRange(article.Symbols);

            await responseStream.WriteAsync(snapshot, cancellationToken);
        }
    }

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
