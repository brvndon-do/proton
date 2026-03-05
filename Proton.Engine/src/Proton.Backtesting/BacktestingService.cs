using Microsoft.Extensions.Hosting;

namespace Proton.Engine.Backtesting;

public class BacktestingService : BackgroundService
{
    protected override Task ExecuteAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
