using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Proton.Engine.Core.Interfaces;
using Proton.Engine.Core.Models;

namespace Proton.Engine.MarketIngestion;

public class MarketIngestionService(IMarketDataProvider marketDataProvider, ILogger<MarketIngestionService> logger) : BackgroundService
{
    private readonly IMarketDataProvider _marketDataProvider = marketDataProvider;
    private readonly ILogger<MarketIngestionService> _logger = logger;

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        await foreach (Bar bar in _marketDataProvider.StreamBarsAsync(["AAPL", "TSLA"]))
        {
            _logger.LogInformation("Symbol: {s}\nOHLCV: {o}/{h}/{l}/{c}/{v}", bar.Symbol, bar.Open, bar.High, bar.Low, bar.Close, bar.Volume);
        }
    }
}
