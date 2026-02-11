using Proton.Engine.Core.Interfaces;
using Proton.Engine.Core.Models;
using Alpaca.Markets;
using Microsoft.Extensions.Options;

using ProtonOrderStatus = Proton.Engine.Core.Models.OrderStatus;
using AlpacaMarkets = Alpaca.Markets;

namespace Proton.Engine.Brokers.Alpaca;

public class AlpacaBroker : IBroker
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

    public async Task<bool> CancelOrderAsync(string orderId, CancellationToken cancellationToken = default) => await _tradingClient.CancelOrderAsync(Guid.Parse(orderId), cancellationToken);

    public async Task<OrderResult> CreateOrderAsync(TradeOrder order, CancellationToken cancellationToken = default)
    {
        IOrder orderResult = await _tradingClient.PostOrderAsync(new NewOrderRequest(
            symbol: order.Symbol,
            quantity: OrderQuantity.Fractional(order.Quantity),
            side: (AlpacaMarkets.OrderSide)order.Side,
            type: ConvertOrderType(order.OrderType),
            duration: ConvertTimeInForce(order.TimeInForce)
        ), cancellationToken);

        return new OrderResult
        {
            OrderId = orderResult.OrderId.ToString(),
            Symbol = orderResult.Symbol,
            Quantity = orderResult.Quantity,
            SubmittedAt = orderResult.SubmittedAtUtc ?? DateTimeOffset.UtcNow,
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

    public async Task<ProtonOrderStatus> GetOrderStatusAsync(string orderId, CancellationToken cancellationToken = default)
    {
        IOrder orderResult = await _tradingClient.GetOrderAsync(Guid.Parse(orderId), cancellationToken);

        return new ProtonOrderStatus
        {
            OrderId = orderId,
        };
    }

    public Task<IEnumerable<Trade>> GetTradeHistoryAsync(DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    private AlpacaMarkets.OrderType ConvertOrderType(Core.Models.OrderType type) => type switch
    {
        Core.Models.OrderType.Market => AlpacaMarkets.OrderType.Market,
        Core.Models.OrderType.Limit => AlpacaMarkets.OrderType.Limit,
        Core.Models.OrderType.StopLimit => AlpacaMarkets.OrderType.StopLimit,
        Core.Models.OrderType.Stop => AlpacaMarkets.OrderType.Stop,
        _ => AlpacaMarkets.OrderType.Market
    };

    private AlpacaMarkets.TimeInForce ConvertTimeInForce(Core.Models.TimeInForce timeInForce) => timeInForce switch
    {
        Core.Models.TimeInForce.Day => AlpacaMarkets.TimeInForce.Day,
        Core.Models.TimeInForce.Gtc => AlpacaMarkets.TimeInForce.Gtc,
        Core.Models.TimeInForce.Ioc => AlpacaMarkets.TimeInForce.Ioc,
        Core.Models.TimeInForce.Fok => AlpacaMarkets.TimeInForce.Fok,
        _ => AlpacaMarkets.TimeInForce.Day
    };
}
