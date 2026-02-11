using Proton.Engine.Core.Interfaces;
using Proton.Engine.Core.Models;

namespace Proton.Engine.Core.Services;

public class AccountService(IBroker broker)
{
    private readonly IBroker _broker = broker;

    public Task<Account> GetAccountAsync(CancellationToken cancellationToken = default) => _broker.GetAccountAsync(cancellationToken);

    public Task<IEnumerable<Position>> GetPositionsAsync(CancellationToken cancellationToken = default) => _broker.GetOpenPositionsAsync(cancellationToken);
}
