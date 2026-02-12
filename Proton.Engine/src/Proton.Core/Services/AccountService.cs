using Proton.Engine.Core.Interfaces;
using Proton.Engine.Core.Models;

namespace Proton.Engine.Core.Services;

public class AccountService(IBroker broker)
{
    private readonly IBroker _broker = broker;

    public Task<Account> GetAccountAsync(CancellationToken cancellationToken = default) => _broker.GetAccountAsync(cancellationToken);

    public async Task<IReadOnlyList<Position>> GetOpenPositionsAsync(CancellationToken cancellationToken = default)
    {
        List<Position> positions = [.. await _broker.GetOpenPositionsAsync(cancellationToken)];
        return positions;
    }

    public async Task<IReadOnlyList<Trade>> GetTradeHistoryAsync(DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken = default)
    {
        List<Trade> trades = [.. await _broker.GetTradeHistoryAsync(from, to, cancellationToken)];
        return trades;
    }
}
