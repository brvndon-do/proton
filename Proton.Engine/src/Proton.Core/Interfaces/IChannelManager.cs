using System.Threading.Channels;
using Proton.Engine.Core.Models.MarketData;

namespace Proton.Engine.Core.Interfaces;

public interface IChannelManager
{
    public Channel<MarketDataRequest> MarketDataRequestChannel { get; }
    public Channel<MarketDataSnapshot> MarketDataSnapshotChannel { get; }
    public Channel<MarketNewsSnapshot> MarketNewsSnapshotChannel { get; }
}
