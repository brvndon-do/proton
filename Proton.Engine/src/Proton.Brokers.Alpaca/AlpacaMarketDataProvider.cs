using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Proton.Engine.Core.Interfaces;
using Alpaca.Markets;
using Proton.Engine.Core.Models;
using System.Threading.Channels;
using System.Runtime.CompilerServices;

namespace Proton.Engine.Brokers.Alpaca;

public class AlpacaMarketDataProvider : IMarketDataProvider
{
    private readonly IAlpacaDataStreamingClient _streamingClient;
    private readonly ILogger<AlpacaMarketDataProvider> _logger;

    public AlpacaMarketDataProvider(IOptions<AlpacaOptions> options, ILogger<AlpacaMarketDataProvider> logger)
    {
        AlpacaOptions _options = options.Value;

        IEnvironment tradingEnvironment = _options.IsPaperAccount
            ? Environments.Paper
            : Environments.Live;

        _streamingClient = tradingEnvironment.GetAlpacaDataStreamingClient(new SecretKey(_options.ApiKey, _options.ApiSecret));
        _logger = logger;
    }

    public async IAsyncEnumerable<Bar> StreamBarsAsync(IEnumerable<string> symbols, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        AuthStatus status = await _streamingClient.ConnectAndAuthenticateAsync(cancellationToken);

        if (status != AuthStatus.Authorized)
            yield break;

        // TODO: potentially switch to bounded channel instead. monitor this.
        Channel<Bar> channel = Channel.CreateUnbounded<Bar>();
        IEnumerable<IAlpacaDataSubscription<IBar>> data = symbols.Select(_streamingClient.GetDailyBarSubscription);

        foreach (IAlpacaDataSubscription<IBar> sub in data)
        {
            sub.Received += (quote) =>
            {
                channel.Writer.TryWrite(new Bar
                {
                    Symbol = quote.Symbol,
                    Open = quote.Open,
                    High = quote.High,
                    Low = quote.Low,
                    Close = quote.Close,
                    Volume = quote.Volume,
                    Vwap = quote.Vwap,
                    TradeCount = quote.TradeCount
                });
            };
        }

        await _streamingClient.SubscribeAsync(data);

        await foreach (Bar bar in channel.Reader.ReadAllAsync())
        {
            yield return bar;
        }
    }
}
