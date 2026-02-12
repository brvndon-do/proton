using Proton.Engine.Core.Models;

namespace Proton.Engine.Core.Interfaces;

public interface IBroker
{
    Task<Account> GetAccountAsync(CancellationToken cancellationToken = default);

    Task<OrderResult> CreateOrderAsync(TradeOrder order, CancellationToken cancellationToken = default);
    Task<bool> CancelOrderAsync(string orderId, CancellationToken cancellationToken = default);

    Task<IEnumerable<Position>> GetOpenPositionsAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<Trade>> GetTradeHistoryAsync(DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken = default);
}
