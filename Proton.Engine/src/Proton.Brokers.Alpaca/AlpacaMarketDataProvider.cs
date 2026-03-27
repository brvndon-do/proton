using System.Runtime.CompilerServices;
using System.Threading.Channels;
using Microsoft.Extensions.Options;
using Proton.Engine.Brokers.Alpaca.Utilities;
using Proton.Engine.Core.Interfaces;
using Proton.Engine.Core.Models;
using Proton.Engine.Core.Models.MarketData;
using Alpaca.Markets;

using ProtonTrading = Proton.Engine.Core.Models.Trading;

namespace Proton.Engine.Brokers.Alpaca;

public class AlpacaMarketDataProvider : IMarketDataProvider
{
    private readonly IAlpacaDataClient _dataClient;
    private readonly IAlpacaDataStreamingClient _dataStreamingClient;
    private readonly IAlpacaNewsStreamingClient _newsStreamingClient;

    public AlpacaMarketDataProvider(IOptions<AlpacaOptions> options)
    {
        AlpacaOptions _options = options.Value;

        IEnvironment tradingEnvironment = _options.IsPaperAccount
            ? Environments.Paper
            : Environments.Live;
        SecretKey key = new SecretKey(_options.ApiKey, _options.ApiSecret);

        _dataClient = tradingEnvironment.GetAlpacaDataClient(key);
        _dataStreamingClient = tradingEnvironment.GetAlpacaDataStreamingClient(key);
        _newsStreamingClient = tradingEnvironment.GetAlpacaNewsStreamingClient(key);
    }

    public async IAsyncEnumerable<Bar> StreamBarsAsync(IEnumerable<string> symbols, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        AuthStatus status = await _dataStreamingClient.ConnectAndAuthenticateAsync(cancellationToken);

        if (status != AuthStatus.Authorized)
            yield break;

        Channel<Bar> channel = Channel.CreateBounded<Bar>(1_000);
        IEnumerable<IAlpacaDataSubscription<IBar>> data = symbols.Select(_dataStreamingClient.GetDailyBarSubscription);

        foreach (IAlpacaDataSubscription<IBar> sub in data)
        {
            sub.Received += (quote) =>
            {
                _ = channel.Writer.WriteAsync(quote.ToCore(), cancellationToken);
            };
        }

        await _dataStreamingClient.SubscribeAsync(data, cancellationToken);

        await foreach (Bar bar in channel.Reader.ReadAllAsync(cancellationToken))
        {
            yield return bar;
        }
    }

    public async IAsyncEnumerable<NewsArticle> StreamNewsDataAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        AuthStatus status = await _newsStreamingClient.ConnectAndAuthenticateAsync(cancellationToken);

        if (status != AuthStatus.Authorized)
            yield break;

        Channel<NewsArticle> channel = Channel.CreateBounded<NewsArticle>(new BoundedChannelOptions(100)
        {
            FullMode = BoundedChannelFullMode.DropOldest, // NOTE: news market data is still good for context, but prob not as impactful? ok to drop i think.
        });

        IAlpacaDataSubscription<INewsArticle> data = _newsStreamingClient.GetNewsSubscription();

        data.Received += article =>
        {
            _ = channel.Writer.WriteAsync(article.ToCore(), cancellationToken);
        };

        await _newsStreamingClient.SubscribeAsync(data, cancellationToken);

        await foreach (NewsArticle article in channel.Reader.ReadAllAsync(cancellationToken))
        {
            yield return article;
        }
    }

    public async Task<IEnumerable<NewsArticle>> GetNewsDataAsync(MarketNewsRequest request, CancellationToken cancellationToken = default)
    {
        int limit = request.Limit > 0 ? request.Limit : 10;
        IPage<INewsArticle> articles = await _dataClient.ListNewsArticlesAsync(new NewsArticlesRequest(request.Symbols)
        {
            TimeInterval = new Interval<DateTime>(request.StartInterval, request.EndInterval)
        }, cancellationToken);

        return articles.Items
            .Take(limit)
            .Select(x => x.ToCore());
    }

    public async Task<IEnumerable<Bar>> GetHistoricalBarsAsync(IEnumerable<string> symbols, ProtonTrading.TimeFrame timeFrame, DateTime? from, DateTime? to, int limit = 1000, CancellationToken cancellationToken = default)
    {
        IPage<IBar> historicalBars = await _dataClient.ListHistoricalBarsAsync(new HistoricalBarsRequest(
            symbols: symbols,
            timeFrame: timeFrame.ToAlpaca(),
            timeInterval: new Interval<DateTime>(from, to)
        ), cancellationToken);

        return historicalBars.Items
            .Take(limit)
            .Select(x => x.ToCore());
    }
}
