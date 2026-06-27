using System.Runtime.CompilerServices;
using System.Threading.Channels;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using Proton.Engine.Brokers.Alpaca.Utilities;
using Proton.Engine.Core.Interfaces;
using Proton.Engine.Core.Models;
using Alpaca.Markets;

using ProtonTrading = Proton.Engine.Core.Models.Trading;

namespace Proton.Engine.Brokers.Alpaca;

public class AlpacaMarketDataProvider : IMarketDataProvider
{
    private readonly IAlpacaDataClient _dataClient;
    private readonly IAlpacaDataStreamingClient _dataStreamingClient;

    // TODO: uncomment, for now this isn't needed
    // private readonly ILogger<AlpacaMarketDataProvider> _logger;

    private readonly Channel<Bar> _barChannel;
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

        _barChannel = Channel.CreateBounded<Bar>(1_000);
    }

    public async Task ConnectAsync(CancellationToken cancellationToken = default)
    {
        if (_isConnected)
            return;

        AuthStatus dataStatus = await _dataStreamingClient.ConnectAndAuthenticateAsync(cancellationToken);

        if (dataStatus != AuthStatus.Authorized)
            throw new InvalidOperationException("Failed to authenicate");

        _isConnected = true;
    }

    public async Task DisconnectAsync(CancellationToken cancellationToken = default)
    {
        await _dataStreamingClient.DisconnectAsync();

        _barChannel.Writer.TryComplete();

        _isConnected = false;
    }

    public async Task SubscribeToSymbolAsync(string symbol, CancellationToken cancellationToken = default)
    {
        IAlpacaDataSubscription<IBar> dataSubscription = _dataStreamingClient.GetDailyBarSubscription(symbol);

        dataSubscription.Received += bar =>
        {
            _barChannel.Writer.TryWrite(bar.ToCore());
        };

        await _dataStreamingClient.SubscribeAsync(dataSubscription, cancellationToken);
    }

    public async Task UnsubscribeToSymbolAsync(string symbol, CancellationToken cancellationToken = default)
    {
        IAlpacaDataSubscription<IBar> dataSubscription = _dataStreamingClient.GetDailyBarSubscription(symbol);

        await _dataStreamingClient.UnsubscribeAsync(dataSubscription, cancellationToken);
    }

    public async IAsyncEnumerable<Bar> StreamBarsAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await foreach (Bar bar in _barChannel.Reader.ReadAllAsync(cancellationToken))
            yield return bar;
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

    public Task<IEnumerable<Bar>> GetHistoricalBarsAsync(string symbol, ProtonTrading.TimeFrame timeFrame, DateTime? from, DateTime? to, int limit = 1000, CancellationToken cancellationToken = default) =>
        GetHistoricalBarsAsync([symbol], timeFrame, from, to, limit, cancellationToken);
}
