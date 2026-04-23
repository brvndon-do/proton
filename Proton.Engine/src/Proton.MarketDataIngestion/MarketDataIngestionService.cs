using System.Threading.Channels;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Proton.Engine.Core.Interfaces;
using Proton.Engine.Core.Models;
using Proton.Engine.Core.Models.MarketData;

namespace Proton.Engine.MarketDataIngestion;

public class MarketDataIngestion(
    IMarketDataSubscriptionManager marketDataSubscriptionManager,
    ILogger<MarketDataIngestion> logger
) : BackgroundService
{
    private readonly IMarketDataSubscriptionManager _marketDataSubscriptionManager = marketDataSubscriptionManager;
    private readonly ILogger<MarketDataIngestion> _logger = logger;

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        _logger.LogDebug("Processing warmup symbols...");

        // TODO: read from different source instead of hard code
        string[] symbols = ["AAPL", "TSLA", "NVDA", "META"];

        // NOTE: drain unused channels
        foreach (string symbol in symbols)
        {
            Channel<Bar> channel = await _marketDataSubscriptionManager.SubscribeAsync(symbol, cancellationToken: cancellationToken);

            // TODO: maybe better way to do this?
            _ = Task.Run(async () =>
            {
                await foreach (Bar _ in channel.Reader.ReadAllAsync(cancellationToken)) { }
            }, cancellationToken);
        }
    }
}
