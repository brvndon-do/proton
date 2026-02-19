using System.Threading.Channels;

namespace Proton.Engine.Core.Models.MarketData;

public class MarketDataContext
{
    public required MarketDataRequest Request { get; init; }
    public required Channel<MarketDataSnapshot> MarketDataResponseChannel { get; init; }
}
