using System.Threading.Channels;

namespace Proton.Engine.Core.Models.MarketData;

public class MarketNewsContext
{
    public required Channel<MarketNewsSnapshot> MarketNewsChannel { get; init; }
}
