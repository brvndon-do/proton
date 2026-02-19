using System.Threading.Channels;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Proton.Engine.AppHost.Grpc;
using Proton.Engine.AppHost.Utilities;
using Proton.Engine.Core.Interfaces;
using Proton.Engine.Core.Models;
using Proton.Engine.Core.Models.MarketData;

namespace Proton.Engine.AppHost.Services;

public class MarketDataService(
    IChannelManager channelManager,
    IMarketDataProvider marketDataProvider,
    ILogger<MarketDataService> logger
) : MarketData.MarketDataBase
{
    private readonly IChannelManager _channelManager = channelManager;
    private readonly IMarketDataProvider _marketDataProvider = marketDataProvider;
    private readonly ILogger<MarketDataService> _logger = logger;

    public override async Task StreamMarketSnapshot(MarketSnapshotRequest request, IServerStreamWriter<MarketSnapshot> responseStream, ServerCallContext context)
    {
        CancellationToken cancellationToken = context.CancellationToken;
        Channel<MarketDataSnapshot> responseChannel = Channel.CreateBounded<MarketDataSnapshot>(1_000);

        await _channelManager.MarketDataContextChannel.Writer.WriteAsync(new MarketDataContext
        {
            Request = request.ToCore(),
            MarketDataResponseChannel = responseChannel,
        }, cancellationToken);

        try
        {
            await foreach (MarketDataSnapshot snapshot in responseChannel.Reader.ReadAllAsync(cancellationToken))
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

    public override async Task StreamNewsSnapshot(Empty request, IServerStreamWriter<NewsSnapshot> responseStream, ServerCallContext context)
    {
        CancellationToken cancellationToken = context.CancellationToken;
        Channel<MarketNewsSnapshot> responseChannel = Channel.CreateBounded<MarketNewsSnapshot>(100);

        await _channelManager.MarketNewsContextChannel.Writer.WriteAsync(new MarketNewsContext
        {
            MarketNewsChannel = responseChannel,
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
                CreatedAt = Timestamp.FromDateTimeOffset(article.CreatedAtUtc)
            };
            snapshot.Symbols.AddRange(article.Symbols);

            await responseStream.WriteAsync(snapshot, cancellationToken);
        }
    }
}
