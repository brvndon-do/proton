using Microsoft.Extensions.Options;
using Proton.Engine.Core.Interfaces;
using Alpaca.Markets;
using Proton.Engine.Core.Models;
using System.Threading.Channels;
using System.Runtime.CompilerServices;
using Proton.Engine.Core.Models.MarketData;

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
                }, cancellationToken);
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
            _ = channel.Writer.WriteAsync(new NewsArticle
            {
                Id = article.Id.ToString(),
                Headline = article.Headline,
                Summary = article.Summary,
                Content = article.Content,
                Author = article.Author,
                Source = article.Source,
                Symbols = article.Symbols,
                CreatedAtUtc = article.CreatedAtUtc,
            }, cancellationToken);
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
            .Select(x => new NewsArticle
            {
                Id = x.Id.ToString(),
                Headline = x.Headline,
                Summary = x.Summary,
                Content = x.Content,
                Author = x.Author,
                Source = x.Source,
                Symbols = x.Symbols,
                CreatedAtUtc = x.CreatedAtUtc,
            });
    }
}
