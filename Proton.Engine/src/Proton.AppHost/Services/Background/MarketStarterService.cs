using Proton.Engine.Core.Interfaces;
using Proton.Engine.Core.Models.MarketData;

namespace Proton.Engine.AppHost.Services.Background;

public class MarketStarterService(IChannelManager channelManager) : BackgroundService
{
    private readonly IChannelManager _channelManager = channelManager;

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        // TODO: read from database or configuration file for domain data
        // for now, read from hard-coded list of symbols

        string[] symbols = ["AAPL", "TSLA", "NVDA", "SPY"];

        await _channelManager.MarketDataContextChannel.Writer.WriteAsync(new MarketDataContext
        {
            Request = new MarketDataRequest
            {
                Symbols = symbols
            }
        }, cancellationToken);

        // TODO: start news service here, so it can add in redis cache
    }
}
