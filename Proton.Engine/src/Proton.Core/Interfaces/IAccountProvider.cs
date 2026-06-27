using Proton.Engine.Core.Models;
using Proton.Engine.Core.Models.Trading;

namespace Proton.Engine.Core.Interfaces;

public interface IAccountProvider
{
    Task<Account> GetAccountAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<Position>> GetOpenPositionsAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<Trade>> GetTradeHistoryAsync(DateTime from, DateTime to, CancellationToken cancellationToken = default);
}
