using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Proton.Engine.Core.Interfaces;

namespace Proton.Engine.MarketDataIngestion;

public class MarketDataIngestion(
    IServiceScopeFactory serviceScopeFactory,
    IMarketDataSubscriptionManager marketDataSubscriptionManager,
    ILogger<MarketDataIngestion> logger
) : BackgroundService
{
    private readonly IServiceScopeFactory _serviceScopeFactory = serviceScopeFactory;
    private readonly IMarketDataSubscriptionManager _marketDataSubscriptionManager = marketDataSubscriptionManager;
    private readonly ILogger<MarketDataIngestion> _logger = logger;

    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        using IServiceScope serviceScope = _serviceScopeFactory.CreateScope();

        IEnumerable<IMarketDataProvider> providers = serviceScope.ServiceProvider.GetServices<IMarketDataProvider>();
        foreach (IMarketDataProvider provider in providers)
        {
            await provider.ConnectAsync();
        }

        await base.StartAsync(cancellationToken);
    }

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        _logger.LogDebug("Processing warmup symbols...");

        // TODO: read from different source instead of hard code
        string[] symbols = ["AAPL", "TSLA", "NVDA", "META"];

        foreach (string symbol in symbols)
        {
            await _marketDataSubscriptionManager.PinAsync(symbol, cancellationToken);
        }
    }
}
