using Proton.Engine.AppHost.Services;
using Proton.Engine.Brokers.Alpaca;
using Proton.Engine.Core.Interfaces;
using Proton.Engine.Core.Services;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddGrpc();

// brokers
builder.Services.AddSingleton<IBroker, AlpacaBroker>();

// core services
builder.Services.AddSingleton<TradeExecutionService>();

WebApplication app = builder.Build();

app.MapGrpcService<TradingService>();

app.MapGet("/", () => "Communication with gRPC endpoints must be made through a gRPC client. To learn how to create a client, visit: https://go.microsoft.com/fwlink/?linkid=2086909");

app.Run();
