using Proton.Engine.Core.Models;

namespace Proton.Engine.Core.Interfaces.Repositories;

public interface IBarRepository : IRepository<Bar>
{
    // TODO: IBarRepository may need to look at why it implements from IRepository<T>..

    Task<IEnumerable<Bar>> ReadBarsAsync(string symbol); // TODO: pass in condition filter?
}
