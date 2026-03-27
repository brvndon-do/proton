using ProtonModels = Proton.Engine.Core.Models;
using ProtonTradingModels = Proton.Engine.Core.Models.Trading;
using AlpacaMarkets = Alpaca.Markets;

namespace Proton.Engine.Brokers.Alpaca.Utilities;

public static class ModelMapper
{
    public static AlpacaMarkets.OrderType ToAlpaca(this ProtonTradingModels.OrderType type) => type switch
    {
        ProtonTradingModels.OrderType.Market => AlpacaMarkets.OrderType.Market,
        ProtonTradingModels.OrderType.Limit => AlpacaMarkets.OrderType.Limit,
        ProtonTradingModels.OrderType.StopLimit => AlpacaMarkets.OrderType.StopLimit,
        ProtonTradingModels.OrderType.Stop => AlpacaMarkets.OrderType.Stop,
        _ => AlpacaMarkets.OrderType.Market
    };

    public static AlpacaMarkets.TimeInForce ToAlpaca(this ProtonTradingModels.TimeInForce timeInForce) => timeInForce switch
    {
        ProtonTradingModels.TimeInForce.Day => AlpacaMarkets.TimeInForce.Day,
        ProtonTradingModels.TimeInForce.Gtc => AlpacaMarkets.TimeInForce.Gtc,
        ProtonTradingModels.TimeInForce.Ioc => AlpacaMarkets.TimeInForce.Ioc,
        ProtonTradingModels.TimeInForce.Fok => AlpacaMarkets.TimeInForce.Fok,
        _ => AlpacaMarkets.TimeInForce.Day
    };

    public static AlpacaMarkets.BarTimeFrame ToAlpaca(this ProtonTradingModels.TimeFrame timeFrame) => timeFrame switch
    {
        ProtonTradingModels.TimeFrame.Hourly => AlpacaMarkets.BarTimeFrame.Hour,
        ProtonTradingModels.TimeFrame.Daily => AlpacaMarkets.BarTimeFrame.Day,
        _ => AlpacaMarkets.BarTimeFrame.Day
    };

    public static ProtonModels.NewsArticle ToCore(this AlpacaMarkets.INewsArticle article) => new ProtonModels.NewsArticle
    {
        Id = article.Id.ToString(),
        Headline = article.Headline,
        Summary = article.Summary,
        Content = article.Content,
        Author = article.Author,
        Source = article.Source,
        Symbols = article.Symbols,
        CreatedAtUtc = article.CreatedAtUtc,
    };

    public static ProtonModels.Bar ToCore(this AlpacaMarkets.IBar bar) => new ProtonModels.Bar
    {
        Symbol = bar.Symbol,
        Open = bar.Open,
        High = bar.High,
        Low = bar.Low,
        Close = bar.Close,
        Volume = bar.Volume,
        Vwap = bar.Vwap,
        TradeCount = bar.TradeCount,
        DateTimeUtc = bar.TimeUtc,
    };
}
