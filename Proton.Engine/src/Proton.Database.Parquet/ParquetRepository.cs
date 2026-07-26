using System.Collections.Concurrent;
using Parquet.Serialization;
using Proton.Engine.Core.Interfaces.Repositories;
using Proton.Engine.Core.Models;

namespace Proton.Engine.Database.Parquet;

public class ParquetRepository : IBarRepository
{
    private static readonly string PARQUET_FILE_DIR = Path.Combine(AppContext.BaseDirectory, "output");
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> _fileLocks = new ConcurrentDictionary<string, SemaphoreSlim>();

    private static SemaphoreSlim LockFile(string symbol) => _fileLocks.GetOrAdd(symbol, _ => new SemaphoreSlim(initialCount: 1, maxCount: 1));

    public async Task AddAsync(Bar entity, CancellationToken cancellationToken = default)
    {
        SemaphoreSlim gate = LockFile(entity.Symbol);
        await gate.WaitAsync(cancellationToken);

        try
        {
            (FileStream fs, bool exists) = GetFileStream(entity.Symbol);

            using (fs)
            {
                await ParquetSerializer.SerializeAsync(
                    objectInstances: [entity],
                    destination: fs,
                    options: new ParquetSerializerOptions { Append = exists },
                    cancellationToken: cancellationToken
                );
            }
        }
        finally
        {
            gate.Release();
        }
    }

    public async Task AddRangeAsync(IEnumerable<Bar> entities, CancellationToken cancellationToken = default)
    {
        foreach (IGrouping<string, Bar> bars in entities.GroupBy(x => x.Symbol))
        {
            SemaphoreSlim gate = LockFile(bars.Key);
            await gate.WaitAsync(cancellationToken);

            try
            {
                (FileStream fs, bool exists) = GetFileStream(bars.Key);

                using (fs)
                {
                    await ParquetSerializer.SerializeAsync(
                        objectInstances: bars,
                        destination: fs,
                        options: new ParquetSerializerOptions { Append = exists },
                        cancellationToken: cancellationToken
                    );
                }
            }
            finally
            {
                gate.Release();
            }
        }
    }

    public async Task RemoveByKeyAsync(string key, CancellationToken cancellationToken = default)
    {
        SemaphoreSlim gate = LockFile(key);
        await gate.WaitAsync(cancellationToken);

        try
        {
            string path = Path.Combine(PARQUET_FILE_DIR, $"{key}.parquet");

            if (File.Exists(path))
                File.Delete(path);
        }
        finally
        {
            gate.Release();
        }
    }

    public async Task<IEnumerable<Bar>> ReadBarsAsync(string symbol, CancellationToken cancellationToken = default)
    {
        IList<Bar> barsRead = [];
        SemaphoreSlim gate = LockFile(symbol);
        await gate.WaitAsync(cancellationToken);

        try
        {
            (FileStream fs, _) = GetFileStream(symbol, write: false);

            using (fs)
            {
                if (fs.Length == 0)
                    return [];

                barsRead = await ParquetSerializer.DeserializeAsync<Bar>(fs, cancellationToken: cancellationToken);
            }
        }
        finally
        {
            gate.Release();
        }

        return barsRead;
    }

    private (FileStream fileStream, bool fileExists) GetFileStream(string filename, bool write = true)
    {
        if (!Directory.Exists(PARQUET_FILE_DIR))
            Directory.CreateDirectory(PARQUET_FILE_DIR);

        string path = Path.Combine(PARQUET_FILE_DIR, $"{filename}.parquet");

        FileStream fs = new FileStream(
            path,
            FileMode.OpenOrCreate,
            write ? FileAccess.ReadWrite : FileAccess.Read
        );

        bool exists = File.Exists(path) && fs.Length > 0;

        return (
            fileStream: fs,
            fileExists: exists
        );
    }
}
