using System.Text.Json;
using Microsoft.Extensions.Options;
using Proton.Engine.Core.Interfaces.Repositories;
using Proton.Engine.Core.Models;
using StackExchange.Redis;

namespace Proton.Engine.Database.Redis;

public class RedisRepository(
    IConnectionMultiplexer connectionMultiplexer,
    IOptions<RedisOptions> options
) : ICacheRepository
{
    private readonly IDatabase _db = connectionMultiplexer.GetDatabase();
    private readonly RedisOptions _options = options.Value;

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
        string symbol = entity.Symbol;
        string json = JsonSerializer.Serialize(entity);
        await _db.SortedSetAddAsync(Key(symbol), json, Score(entity));

        // TTL/trimming
        await _db.SortedSetRemoveRangeByRankAsync(Key(symbol), 0, -(_options.MaxBarsPerSymbol + 1));
    }

    public async Task AddRangeAsync(IEnumerable<Bar> entities, CancellationToken cancellationToken = default)
    {
        IEnumerable<IGrouping<string, Bar>> sortedEntities = entities.GroupBy(x => x.Symbol);

        foreach (IGrouping<string, Bar> bars in sortedEntities)
        {
            string symbol = bars.Key;

            SortedSetEntry[] entries = [.. bars.Select(x => new SortedSetEntry(JsonSerializer.Serialize(x), Score(x)))];
            await _db.SortedSetAddAsync(Key(symbol), entries);

            // TTL/trimming
            await _db.SortedSetRemoveRangeByRankAsync(Key(symbol), 0, -(_options.MaxBarsPerSymbol + 1));
        }
    }

    public async Task RemoveByKeyAsync(string key, CancellationToken cancellationToken = default) => await _db.KeyDeleteAsync(Key(key));
}
