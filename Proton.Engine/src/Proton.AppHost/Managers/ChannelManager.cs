using System.Threading.Channels;
using Proton.Engine.Core.Interfaces;
using Proton.Engine.Core.Models.MarketData;

namespace Proton.Engine.AppHost.Managers;

public class ChannelManager : IChannelManager
{
    private readonly Channel<MarketDataContext> _marketDataContext = Channel.CreateBounded<MarketDataContext>(1_000);
    private readonly Channel<MarketNewsContext> _marketNewsContext = Channel.CreateBounded<MarketNewsContext>(100);

    public Channel<MarketDataContext> MarketDataContextChannel => _marketDataContext;
    public Channel<MarketNewsContext> MarketNewsContextChannel => _marketNewsContext;
}
