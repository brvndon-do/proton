using System.Threading.Channels;
using Proton.Engine.Core.Interfaces;
using Proton.Engine.Core.Models.MarketData;

namespace Proton.Engine.AppHost.Managers;

public class ChannelManager : IChannelManager
{
    public Channel<MarketDataRequest> MarketDataRequestChannel => Channel.CreateBounded<MarketDataRequest>(1_000);
    public Channel<MarketDataSnapshot> MarketDataSnapshotChannel => Channel.CreateBounded<MarketDataSnapshot>(1_000);

    public Channel<MarketNewsSnapshot> MarketNewsSnapshotChannel => Channel.CreateBounded<MarketNewsSnapshot>(100);

}
