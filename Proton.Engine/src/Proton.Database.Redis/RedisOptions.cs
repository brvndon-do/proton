namespace Proton.Engine.Database.Redis;

public sealed class RedisOptions
{
    public const string SectionName = nameof(RedisOptions);
    public required string Configuration { get; init; }
    public int MaxBarsPerSymbol { get; init; }
}
