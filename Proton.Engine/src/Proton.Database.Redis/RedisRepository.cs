using System.Text.Json;
using Proton.Engine.Core.Interfaces.Repositories;
using Proton.Engine.Core.Models;
using StackExchange.Redis;

namespace Proton.Engine.Database.Redis;

public class RedisRepository(IConnectionMultiplexer connectionMultiplexer) : ICacheRepository
{
    private readonly IDatabase _db = connectionMultiplexer.GetDatabase();

    private static string Key(string symbol) => $"bars:{symbol}";
    private static double Score(Bar bar) => bar.DateTimeUtc.Ticks;

    public async Task<IEnumerable<Bar>> GetLatestBarsAsync(string symbol, int window, CancellationToken cancellationToken = default)
    {
        if (window <= 0)
            return [];

        RedisValue[] results = await _db.SortedSetRangeByRankAsync(
            Key(symbol),
            start: 0,
            stop: window - 1,
            order: Order.Descending
        );

        return results
            .Select(x => JsonSerializer.Deserialize<Bar>(x.ToString())!)
            .Reverse();
    }

    public async Task AddAsync(Bar entity, CancellationToken cancellationToken = default)
    {
        string json = JsonSerializer.Serialize(entity);
        await _db.SortedSetAddAsync(Key(entity.Symbol), json, Score(entity));
    }

    public async Task AddRangeAsync(IEnumerable<Bar> entities, CancellationToken cancellationToken = default)
    {
        IEnumerable<IGrouping<string, Bar>> sortedEntities = entities.GroupBy(x => x.Symbol);

        foreach (IGrouping<string, Bar> bars in sortedEntities)
        {
            SortedSetEntry[] entries = [.. bars.Select(x => new SortedSetEntry(JsonSerializer.Serialize(x), Score(x)))];
            await _db.SortedSetAddAsync(Key(bars.Key), entries);
        }
    }

    public async Task RemoveByKeyAsync(string key, CancellationToken cancellationToken = default) => await _db.KeyDeleteAsync(Key(key));
}
