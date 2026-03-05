namespace Proton.Engine.Core.Interfaces.Repositories;

// TODO: don't know if good design pattern, but finna roll with it
public interface IRepository<TKey, TEntity>
{
    Task<TEntity?> GetByKeyAsync(TKey key, CancellationToken cancellationToken = default);

    Task AddAsync(TEntity entity, CancellationToken cancellationToken = default);
    Task AddRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default);
    Task RemoveByKeyAsync(TKey key, CancellationToken cancellationToken = default);
}
