using Grpc.Core;
using Proton.Engine.AppHost.Grpc;
using Proton.Engine.AppHost.Utilities;
using Proton.Engine.Core.Interfaces;
using Proton.Engine.Core.Models.MarketData;

namespace Proton.Engine.AppHost.Services;

public class MarketDataService(IChannelManager channelManager) : MarketData.MarketDataBase
{
    private readonly IChannelManager _channelManager = channelManager;

    public override async Task StreamMarketSnapshot(MarketSnapshotRequest request, IServerStreamWriter<MarketSnapshot> responseStream, ServerCallContext context)
    {
        CancellationToken cancellationToken = context.CancellationToken;

        await _channelManager.MarketDataRequestChannel.Writer.WriteAsync(request.ToCore(), cancellationToken);

        await foreach (MarketDataSnapshot snapshot in _channelManager.MarketDataSnapshotChannel.Reader.ReadAllAsync(cancellationToken))
        {
            await responseStream.WriteAsync(snapshot.ToGrpc(), cancellationToken);
        }
    }
}
