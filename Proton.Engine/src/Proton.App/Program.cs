using Proton.Engine.Brokers.Alpaca;
using Proton.Engine.Database.Parquet;
using Proton.Engine.Database.Redis;
using Proton.Engine.Indicators;
using Proton.Engine.MarketDataIngestion;
using Proton.Engine.Repl;
using Proton.Engine.TradeExecution;
using Serilog;

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

// an interactive session owns the console for the REPL; logs go to file only
bool isInteractive = !Console.IsInputRedirected;
string logPath = Path.Combine(builder.Environment.ContentRootPath, "logs", "proton-.log");

builder.Services.AddSerilog(config =>
{
    config
        .ReadFrom.Configuration(builder.Configuration)
        .Enrich.FromLogContext()
        .WriteTo.File(logPath, rollingInterval: RollingInterval.Day);

    if (!isInteractive)
        config.WriteTo.Console();
});

builder.Services
    // module registrations
    .AddProtonTradeServices()
    .AddProtonIndicatorServices()
    .AddProtonMarketDataIngestionServices(builder.Configuration)
    // databases/repositories
    .AddProtonParquetServices()
    .AddProtonRedisServices(builder.Configuration)
    // brokers
    .AddProtonAlpacaBrokerServices(builder.Configuration)
    // repl
    .AddProtonReplService();

IHost host = builder.Build();
host.Run();
