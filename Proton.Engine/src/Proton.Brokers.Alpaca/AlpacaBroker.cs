using Proton.Engine.Core.Models;
using Proton.Engine.Core.Models.MarketData;
using Proton.Engine.Core.Models.Trading;
using Proton.Engine.Core.Interfaces;
using Proton.Engine.Brokers.Alpaca.Utilities;
using Alpaca.Markets;
using Microsoft.Extensions.Options;

namespace Proton.Engine.Brokers.Alpaca;

public class AlpacaBroker : IOrderGateway, IAccountProvider, IMarketClock
{
    private readonly IAlpacaTradingClient _tradingClient;

    public AlpacaBroker(IOptions<AlpacaOptions> options)
    {
        AlpacaOptions _options = options.Value;

        IEnvironment tradingEnvironment = _options.IsPaperAccount
            ? Environments.Paper
            : Environments.Live;

        _tradingClient = tradingEnvironment.GetAlpacaTradingClient(new SecretKey(_options.ApiKey, _options.ApiSecret));
    }

    public Task<bool> CancelOrderAsync(string orderId, CancellationToken cancellationToken = default) => _tradingClient.CancelOrderAsync(Guid.Parse(orderId), cancellationToken);

    public async Task<OrderResult> CreateOrderAsync(TradeOrder order, CancellationToken cancellationToken = default)
    {
        IOrder orderResult = await _tradingClient.PostOrderAsync(new NewOrderRequest(
            symbol: order.Symbol,
            quantity: OrderQuantity.Fractional(order.Quantity),
            side: order.Side.ToAlpaca(),
            type: order.OrderType.ToAlpaca(),
            duration: order.TimeInForce.ToAlpaca()
        ), cancellationToken);

        return new OrderResult
        {
            OrderId = orderResult.OrderId.ToString(),
            Symbol = orderResult.Symbol,
            Quantity = orderResult.Quantity,
            SubmittedAtUtc = orderResult.SubmittedAtUtc ?? DateTime.UtcNow,
            FilledQuantity = orderResult.FilledQuantity,
        };
    }

    public async Task<Account> GetAccountAsync(CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<IEnumerable<Position>> GetOpenPositionsAsync(CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<IEnumerable<Trade>> GetTradeHistoryAsync(DateTime from, DateTime to, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public async Task<MarketClock> GetClockAsync(CancellationToken cancellationToken = default)
    {
        IClock clock = await _tradingClient.GetClockAsync(cancellationToken);

        return new MarketClock
        {
            IsOpen = clock.IsOpen,
            TimestampUtc = clock.TimestampUtc,
            NextOpenUtc = clock.NextOpenUtc,
            NextCloseUtc = clock.NextCloseUtc,
        };
    }

}
