using Proton.Engine.Brokers.Alpaca;
using Proton.Engine.Database.Parquet;
using Proton.Engine.Database.Redis;
using Proton.Engine.Indicators;
using Proton.Engine.MarketDataIngestion;
using Proton.Engine.TradeExecution;

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

// module registrations
builder.Services.AddProtonTradeServices();
builder.Services.AddProtonIndicatorServices();
builder.Services.AddProtonMarketDataIngestionServices(builder.Configuration);

// databases/repositories
builder.Services.AddProtonParquetServices();
builder.Services.AddProtonRedisServices(builder.Configuration);

// brokers
builder.Services.AddProtonAlpacaBrokerServices(builder.Configuration);

IHost host = builder.Build();
host.Run();
