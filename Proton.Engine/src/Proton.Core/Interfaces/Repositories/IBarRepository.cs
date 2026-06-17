using Proton.Engine.Core.Models;

namespace Proton.Engine.Core.Interfaces.Repositories;

public interface IBarRepository : IRepository<string, Bar>
{
    Task<IEnumerable<Bar>> ReadBarsAsync(string symbol, CancellationToken cancellationToken = default); // TODO: pass in condition filter?
}
