using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Proton.Engine.Core.Services;
using Proton.Engine.Brokers.Alpaca;
using Proton.Engine.Core.Interfaces;
using Microsoft.Extensions.Configuration;

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

Console.WriteLine($"ASPNETCORE_ENVIRONMENT = {builder.Environment.EnvironmentName}");

builder.Services
    .AddOptions<AlpacaOptions>()
    .Bind(builder.Configuration.GetSection(AlpacaOptions.SectionName))
    .ValidateOnStart();

builder.Services.AddSingleton<IBroker, AlpacaBroker>();
builder.Services.AddSingleton<TradingService>();

using IHost host = builder.Build();

TradingService tradingService = host.Services.GetRequiredService<TradingService>();
await tradingService.ExecuteTradeAsync();

await host.RunAsync();
