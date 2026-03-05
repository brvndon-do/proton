using Proton.Engine.Core.Interfaces.Repositories;
using Proton.Engine.Core.Models;

namespace Proton.Engine.Database.Redis;

public class RedisRepository : ICacheRepository
{
    public Task AddAsync(Bar entity, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task AddRangeAsync(IEnumerable<Bar> entities, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<Bar?> GetByKeyAsync(string key, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task RemoveByKeyAsync(string key, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}
