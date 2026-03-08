using Proton.Engine.AppHost.Managers;
using Proton.Engine.AppHost.Services.Background;
using Proton.Engine.AppHost.Services.Grpc;
using Proton.Engine.Backtesting;
using Proton.Engine.Brokers.Alpaca;
using Proton.Engine.Core.Interfaces;
using Proton.Engine.Core.Interfaces.Repositories;
using Proton.Engine.Core.Services;
using Proton.Engine.Core.Services.Mock;
using Proton.Engine.Database.Parquet;
using Proton.Engine.Database.Redis;
using Proton.Engine.Indicators;
using Proton.Engine.MarketDataIngestion;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddGrpc();

// appsettings/usersecrets configuration
builder.Services.Configure<AlpacaOptions>(builder.Configuration.GetSection(AlpacaOptions.SectionName));

// managers
builder.Services.AddSingleton<IChannelManager, ChannelManager>();

// brokers
builder.Services.AddSingleton<IBroker, AlpacaBroker>();

// core services
builder.Services.AddSingleton<TradeExecutionService>();
builder.Services.AddSingleton<IIndicatorService, IndicatorService>();

// database repos
builder.Services.AddSingleton<IBarRepository, ParquetRepository>();
builder.Services.AddSingleton<ICacheRepository, RedisRepository>();

// market data providers
// builder.Services.AddSingleton<IMarketDataProvider, AlpacaMarketDataProvider>();
builder.Services.AddSingleton<IMarketDataProvider, MockMarketDataProvider>();

// modules
builder.Services.AddHostedService<MarketStarterService>();

builder.Services.AddHostedService<BacktestingService>();
builder.Services.AddHostedService<MarketDataIngestion>();

WebApplication app = builder.Build();

app.MapGrpcService<TradingService>();
app.MapGrpcService<MarketDataService>();

app.MapGet("/", () => "Communication with gRPC endpoints must be made through a gRPC client. To learn how to create a client, visit: https://go.microsoft.com/fwlink/?linkid=2086909");

app.Run();
