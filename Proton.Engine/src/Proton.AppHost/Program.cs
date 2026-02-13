using Proton.Engine.AppHost.Services;
using Proton.Engine.Backtesting;
using Proton.Engine.Brokers.Alpaca;
using Proton.Engine.Core.Interfaces;
using Proton.Engine.Core.Services;
using Proton.Engine.MarketIngestion;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddGrpc();

// appsettings/usersecrets configuration
builder.Services.Configure<AlpacaOptions>(builder.Configuration.GetSection(AlpacaOptions.SectionName));

// brokers
builder.Services.AddSingleton<IBroker, AlpacaBroker>();

// core services
builder.Services.AddSingleton<TradeExecutionService>();

// market data providers
builder.Services.AddSingleton<IMarketDataProvider, AlpacaMarketDataProvider>();

// modules
// builder.Services.AddHostedService<BacktestingService>();
builder.Services.AddHostedService<MarketIngestionService>();

WebApplication app = builder.Build();

app.MapGrpcService<TradingService>();

app.MapGet("/", () => "Communication with gRPC endpoints must be made through a gRPC client. To learn how to create a client, visit: https://go.microsoft.com/fwlink/?linkid=2086909");

app.Run();
