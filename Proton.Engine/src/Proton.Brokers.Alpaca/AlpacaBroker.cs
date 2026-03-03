using Proton.Engine.Core.Models;
using Proton.Engine.Core.Models.Trading;
using Proton.Engine.Core.Interfaces;
using Alpaca.Markets;
using Microsoft.Extensions.Options;

using ProtonTradingModels = Proton.Engine.Core.Models.Trading;
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

    public Task<bool> CancelOrderAsync(string orderId, CancellationToken cancellationToken = default) => _tradingClient.CancelOrderAsync(Guid.Parse(orderId), cancellationToken);

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

    public async Task<bool> IsMarketOpenAsync(CancellationToken cancellationToken = default) => (await _tradingClient.GetClockAsync(cancellationToken)).IsOpen;

    private AlpacaMarkets.OrderType ConvertOrderType(ProtonTradingModels.OrderType type) => type switch
    {
        ProtonTradingModels.OrderType.Market => AlpacaMarkets.OrderType.Market,
        ProtonTradingModels.OrderType.Limit => AlpacaMarkets.OrderType.Limit,
        ProtonTradingModels.OrderType.StopLimit => AlpacaMarkets.OrderType.StopLimit,
        ProtonTradingModels.OrderType.Stop => AlpacaMarkets.OrderType.Stop,
        _ => AlpacaMarkets.OrderType.Market
    };

    private AlpacaMarkets.TimeInForce ConvertTimeInForce(ProtonTradingModels.TimeInForce timeInForce) => timeInForce switch
    {
        ProtonTradingModels.TimeInForce.Day => AlpacaMarkets.TimeInForce.Day,
        ProtonTradingModels.TimeInForce.Gtc => AlpacaMarkets.TimeInForce.Gtc,
        ProtonTradingModels.TimeInForce.Ioc => AlpacaMarkets.TimeInForce.Ioc,
        ProtonTradingModels.TimeInForce.Fok => AlpacaMarkets.TimeInForce.Fok,
        _ => AlpacaMarkets.TimeInForce.Day
    };
}
