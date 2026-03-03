using Parquet.Serialization;
using Proton.Engine.Core.Interfaces.Repositories;
using Proton.Engine.Core.Models;

namespace Proton.Engine.Database.Parquet;

public class ParquetRepository : IBarRepository
{
    private static readonly string PARQUET_FILE_DIR = Path.Combine(AppContext.BaseDirectory, "output");

    public async Task AddAsync(Bar entity, CancellationToken cancellationToken = default)
    {
        using FileStream fs = GetFileStream(entity.Symbol);

        await ParquetSerializer.SerializeAsync([entity], fs, cancellationToken: cancellationToken);
    }

    public async Task AddRangeAsync(IEnumerable<Bar> entities, CancellationToken cancellationToken = default)
    {
        IEnumerable<IGrouping<string, Bar>> sortedEntities = entities.GroupBy(x => x.Symbol);

        foreach (IGrouping<string, Bar> entity in sortedEntities)
        {
            using FileStream fs = GetFileStream(entity.Key);

            await ParquetSerializer.SerializeAsync(entity, fs, cancellationToken: cancellationToken);
        }
    }

    public IEnumerable<Bar> GetAllAsync(CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<Bar?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public async Task<IEnumerable<Bar>> ReadBarsAsync(string symbol)
    {
        using FileStream fs = GetFileStream(symbol);

        return await ParquetSerializer.DeserializeAsync<Bar>(fs);
    }

    public Task RemoveAsync(string id, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    private FileStream GetFileStream(string filename)
    {
        if (!Directory.Exists(PARQUET_FILE_DIR))
            Directory.CreateDirectory(PARQUET_FILE_DIR);

        FileStream fs = new FileStream(
            Path.Combine(PARQUET_FILE_DIR, $"{filename}.parquet"),
            FileMode.OpenOrCreate,
            FileAccess.ReadWrite
        );

        return fs;
    }
}
