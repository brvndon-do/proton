using System.Threading.Channels;

namespace Proton.Engine.Core.Models.MarketData;

public sealed class MarketNewsContext
{
    public required Channel<MarketNewsSnapshot> MarketNewsResponseChannel { get; init; }
}
