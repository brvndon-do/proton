using Proton.Engine.Core.Models;
using Proton.Engine.Core.Interfaces;

namespace Proton.Engine.Core.Services;

public class TradingService(IBroker broker)
{
    private readonly IBroker _broker = broker;

    public async Task ExecuteTradeAsync()
    {
        await _broker.ExecuteTradeAsync(new TradeOrder
        {
            Symbol = "AAPL",
            Quantity = 1.0m,
            Side = OrderSide.Buy,
            TimeInForce = TimeInForce.Day
        });
    }
}
