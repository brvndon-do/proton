namespace Proton.Engine.Database.Redis;

public sealed class RedisOptions
{
    public static string SectionName = nameof(RedisOptions);

    public int MaxBarsPerSymbol { get; set; }
}
