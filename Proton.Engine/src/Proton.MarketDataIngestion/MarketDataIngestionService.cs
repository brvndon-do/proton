using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Proton.Engine.Core.Interfaces;

namespace Proton.Engine.MarketDataIngestion;

public class MarketDataIngestion(
    IEnumerable<IMarketDataProvider> providers,
    IMarketDataSubscriptionManager marketDataSubscriptionManager,
    ILogger<MarketDataIngestion> logger
) : BackgroundService
{
    private readonly IMarketDataSubscriptionManager _marketDataSubscriptionManager = marketDataSubscriptionManager;
    private readonly ILogger<MarketDataIngestion> _logger = logger;

    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        foreach (IMarketDataProvider provider in providers)
        {
            await provider.ConnectAsync(cancellationToken);
        }

        await base.StartAsync(cancellationToken);
    }

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Processing warmup symbols...");

        // TODO: read from different source instead of hard code
        string[] symbols = ["AAPL", "TSLA", "NVDA", "META"];

        foreach (string symbol in symbols)
        {
            await _marketDataSubscriptionManager.PinAsync(symbol, cancellationToken);
        }
    }
}
