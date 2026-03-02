namespace Proton.Engine.Core.Interfaces.Repositories;

public interface IRepository<T>
{
    Task<T?> GetByIdAsync(string id, CancellationToken cancellationToken = default);
    IEnumerable<T> GetAllAsync(CancellationToken cancellationToken = default);
    Task AddAsync(T entity, CancellationToken cancellationToken = default);
    Task AddRangeAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default);
    Task RemoveAsync(string id, CancellationToken cancellationToken = default);
}
