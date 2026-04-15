using Proton.Engine.Core.Models;

namespace Proton.Engine.Core.Interfaces.Repositories;

public interface ICacheRepository : IRepository<string, Bar>
{
    Task<IEnumerable<Bar>> GetLatestBarsAsync(string symbol, int window, CancellationToken cancellationToken = default);
}
