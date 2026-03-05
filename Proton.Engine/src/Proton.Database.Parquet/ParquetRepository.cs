using Parquet.Serialization;
using Proton.Engine.Core.Interfaces.Repositories;
using Proton.Engine.Core.Models;

namespace Proton.Engine.Database.Parquet;

public class ParquetRepository : IBarRepository
{
    private static readonly string PARQUET_FILE_DIR = Path.Combine(AppContext.BaseDirectory, "output");

    public async Task AddAsync(Bar entity, CancellationToken cancellationToken = default)
    {
        (FileStream fs, bool exists) = GetFileStream(entity.Symbol);

        using (fs)
            await ParquetSerializer.SerializeAsync(
                objectInstances: [entity],
                destination: fs,
                options: new ParquetSerializerOptions { Append = exists },
                cancellationToken: cancellationToken
            );
    }

    public async Task AddRangeAsync(IEnumerable<Bar> entities, CancellationToken cancellationToken = default)
    {
        IEnumerable<IGrouping<string, Bar>> sortedEntities = entities.GroupBy(x => x.Symbol);

        foreach (IGrouping<string, Bar> bars in sortedEntities)
        {
            (FileStream fs, bool exists) = GetFileStream(bars.Key);

            using (fs)
                await ParquetSerializer.SerializeAsync(
                    objectInstances: bars,
                    destination: fs,
                    options: new ParquetSerializerOptions { Append = exists },
                    cancellationToken: cancellationToken
                );
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
        (FileStream fs, _) = GetFileStream(symbol);

        using (fs)
            return await ParquetSerializer.DeserializeAsync<Bar>(fs);
    }

    public Task RemoveAsync(string id, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    private (FileStream fileStream, bool fileExists) GetFileStream(string filename)
    {
        if (!Directory.Exists(PARQUET_FILE_DIR))
            Directory.CreateDirectory(PARQUET_FILE_DIR);

        string path = Path.Combine(PARQUET_FILE_DIR, $"{filename}.parquet");
        bool exists = false;

        if (File.Exists(path))
            exists = true;

        FileStream fs = new FileStream(
            path,
            FileMode.OpenOrCreate,
            FileAccess.ReadWrite
        );

        return (
            fileStream: fs,
            fileExists: exists
        );
    }
}
