using Proton.Engine.Brokers.Alpaca;
using Proton.Engine.Database.Parquet;
using Proton.Engine.Database.Redis;
using Proton.Engine.Indicators;
using Proton.Engine.MarketDataIngestion;
using Proton.Engine.TradeExecution;

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

builder.Services
    // module registrations
    .AddProtonTradeServices()
    .AddProtonIndicatorServices()
    .AddProtonMarketDataIngestionServices(builder.Configuration)
    // databases/repositories
    .AddProtonParquetServices()
    .AddProtonRedisServices(builder.Configuration)
    // brokers
    .AddProtonAlpacaBrokerServices(builder.Configuration);

IHost host = builder.Build();
host.Run();
