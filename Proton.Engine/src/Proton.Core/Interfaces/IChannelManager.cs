using System.Threading.Channels;
using Proton.Engine.Core.Models.MarketData;

namespace Proton.Engine.Core.Interfaces;

public interface IChannelManager
{
    public Channel<MarketDataContext> MarketDataContextChannel { get; }
    public Channel<MarketNewsContext> MarketNewsContextChannel { get; }
}
