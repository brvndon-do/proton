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

    public AlpacaMarketDataProvider(IOptions<AlpacaOptions> options)
    {
        AlpacaOptions _options = options.Value;

        IEnvironment tradingEnvironment = _options.IsPaperAccount
            ? Environments.Paper
            : Environments.Live;

        _streamingClient = tradingEnvironment.GetAlpacaDataStreamingClient(new SecretKey(_options.ApiKey, _options.ApiSecret));
    }

    public async IAsyncEnumerable<Bar> StreamBarsAsync(IEnumerable<string> symbols, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        AuthStatus status = await _streamingClient.ConnectAndAuthenticateAsync(cancellationToken);

        if (status != AuthStatus.Authorized)
            yield break;

        Channel<Bar> channel = Channel.CreateBounded<Bar>(1000);
        IEnumerable<IAlpacaDataSubscription<IBar>> data = symbols.Select(_streamingClient.GetDailyBarSubscription);

        foreach (IAlpacaDataSubscription<IBar> sub in data)
        {
            sub.Received += (quote) =>
            {
                _ = channel.Writer.WriteAsync(new Bar
                {
                    Symbol = quote.Symbol,
                    Open = quote.Open,
                    High = quote.High,
                    Low = quote.Low,
                    Close = quote.Close,
                    Volume = quote.Volume,
                    Vwap = quote.Vwap,
                    TradeCount = quote.TradeCount,
                    DateTimeUtc = quote.TimeUtc,
                });
            };
        }

        await _streamingClient.SubscribeAsync(data);

        await foreach (Bar bar in channel.Reader.ReadAllAsync(cancellationToken))
        {
            yield return bar;
        }
    }
}
