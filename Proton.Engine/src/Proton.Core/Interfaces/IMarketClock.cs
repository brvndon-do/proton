using Proton.Engine.Core.Models.MarketData;

namespace Proton.Engine.Core.Interfaces;

public interface IMarketClock
{
    Task<MarketClock> GetClockAsync(CancellationToken cancellationToken = default);
}
