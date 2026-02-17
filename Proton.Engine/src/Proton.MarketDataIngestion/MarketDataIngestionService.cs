using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Proton.Engine.Core.Interfaces;
using Proton.Engine.Core.Models;
using Proton.Engine.Core.Models.MarketData;

namespace Proton.Engine.MarketDataIngestion;

public class MarketDataIngestion(
    IMarketDataProvider marketDataProvider,
    IChannelManager channelManager,
    ILogger<MarketDataIngestion> logger
) : BackgroundService
{
    private readonly IMarketDataProvider _marketDataProvider = marketDataProvider;
    private readonly IChannelManager _channelManager = channelManager;
    private readonly ILogger<MarketDataIngestion> _logger = logger;

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        await foreach (MarketDataRequest request in _channelManager.MarketDataRequestChannel.Reader.ReadAllAsync())
        {
            await foreach (Bar bar in _marketDataProvider.StreamBarsAsync(request.Symbols, cancellationToken))
            {
                await _channelManager.MarketDataSnapshotChannel.Writer.WriteAsync(new MarketDataSnapshot
                {
                    Symbol = bar.Symbol,
                    TimestampUtc = bar.DateTimeUtc,
                    Open = bar.Open,
                    High = bar.High,
                    Low = bar.Low,
                    Close = bar.Close,
                    Volume = bar.Volume,
                    Indicators = [] // TODO: indicators...
                }, cancellationToken);
            }
        }
    }
}
