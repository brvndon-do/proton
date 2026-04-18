using System.Runtime.CompilerServices;
using System.Threading.Channels;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
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

    // TODO: uncomment, for now this isn't needed
    // private readonly ILogger<AlpacaMarketDataProvider> _logger;

    private readonly Channel<Bar> _barChannel;
    private readonly Channel<NewsArticle> _newsChannel;
    private bool _isConnected = false;

    public AlpacaMarketDataProvider(IOptions<AlpacaOptions> options, ILogger<AlpacaMarketDataProvider> logger)
    {
        AlpacaOptions _options = options.Value;
        // _logger = logger;

        IEnvironment tradingEnvironment = _options.IsPaperAccount
            ? Environments.Paper
            : Environments.Live;
        SecretKey key = new SecretKey(_options.ApiKey, _options.ApiSecret);

        _dataClient = tradingEnvironment.GetAlpacaDataClient(key);
        _dataStreamingClient = tradingEnvironment.GetAlpacaDataStreamingClient(key);
        _newsStreamingClient = tradingEnvironment.GetAlpacaNewsStreamingClient(key);

        _barChannel = Channel.CreateBounded<Bar>(1_000);
        _newsChannel = Channel.CreateBounded<NewsArticle>(1_000);
    }

    public async Task ConnectAsync(CancellationToken cancellationToken = default)
    {
        if (_isConnected)
            return;

        AuthStatus dataStatus = await _dataStreamingClient.ConnectAndAuthenticateAsync(cancellationToken);
        AuthStatus newsStatus = await _newsStreamingClient.ConnectAndAuthenticateAsync(cancellationToken);

        if (dataStatus != AuthStatus.Authorized || newsStatus != AuthStatus.Authorized)
            throw new InvalidOperationException("Failed to authenicate");

        _isConnected = true;
    }

    public async Task DisconnectAsync(CancellationToken cancellationToken = default)
    {
        await _dataStreamingClient.DisconnectAsync();
        await _newsStreamingClient.DisconnectAsync();

        _barChannel.Writer.TryComplete();
        _newsChannel.Writer.TryComplete();

        _isConnected = false;
    }

    public async Task SubscribeToSymbolAsync(string symbol, CancellationToken cancellationToken = default)
    {
        IAlpacaDataSubscription<IBar> dataSubscription = _dataStreamingClient.GetDailyBarSubscription(symbol);
        IAlpacaDataSubscription<INewsArticle> newsSubscription = _newsStreamingClient.GetNewsSubscription(symbol);

        dataSubscription.Received += bar =>
        {
            _barChannel.Writer.TryWrite(bar.ToCore());
        };

        newsSubscription.Received += news =>
        {
            _newsChannel.Writer.TryWrite(news.ToCore());
        };

        await _dataStreamingClient.SubscribeAsync(dataSubscription, cancellationToken);
        await _newsStreamingClient.SubscribeAsync(newsSubscription, cancellationToken);
    }

    public async Task UnsubscribeToSymbolAsync(string symbol, CancellationToken cancellationToken = default)
    {
        IAlpacaDataSubscription<IBar> dataSubscription = _dataStreamingClient.GetDailyBarSubscription(symbol);
        IAlpacaDataSubscription<INewsArticle> newsSubscription = _newsStreamingClient.GetNewsSubscription(symbol);

        await _dataStreamingClient.UnsubscribeAsync(dataSubscription, cancellationToken);
        await _newsStreamingClient.UnsubscribeAsync(newsSubscription, cancellationToken);
    }

    public async IAsyncEnumerable<Bar> StreamBarsAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await foreach (Bar bar in _barChannel.Reader.ReadAllAsync(cancellationToken))
            yield return bar;
    }

    public async IAsyncEnumerable<NewsArticle> StreamNewsDataAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await foreach (NewsArticle news in _newsChannel.Reader.ReadAllAsync(cancellationToken))
            yield return news;
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
