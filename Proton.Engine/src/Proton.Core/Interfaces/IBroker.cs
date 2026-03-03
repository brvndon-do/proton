using Proton.Engine.Core.Models;
using Proton.Engine.Core.Models.Trading;

namespace Proton.Engine.Core.Interfaces;

public interface IBroker
{
    Task<Account> GetAccountAsync(CancellationToken cancellationToken = default);

    Task<OrderResult> CreateOrderAsync(TradeOrder order, CancellationToken cancellationToken = default);
    Task<bool> CancelOrderAsync(string orderId, CancellationToken cancellationToken = default);

    Task<IEnumerable<Position>> GetOpenPositionsAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<Trade>> GetTradeHistoryAsync(DateTime from, DateTime to, CancellationToken cancellationToken = default);

    Task<bool> IsMarketOpenAsync(CancellationToken cancellationToken = default);
}
