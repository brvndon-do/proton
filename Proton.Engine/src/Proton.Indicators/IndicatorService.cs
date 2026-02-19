using Proton.Engine.Core.Interfaces;
using Proton.Engine.Core.Models;
using Skender.Stock.Indicators;

namespace Proton.Engine.Indicators;

public class IndicatorService : IIndicatorService
{
    private readonly Dictionary<IndicatorType, int> _indicatorWindowValues = new Dictionary<IndicatorType, int>
    {
        { IndicatorType.Rsi, 14 },
        { IndicatorType.Sma, 20 }
    };

    public Dictionary<IndicatorType, int> IndicatorWindowValues => _indicatorWindowValues;

    public IEnumerable<decimal> CalculateIndicator(IndicatorType type, IEnumerable<Bar> bars)
    {
        IEnumerable<Quote> quotes = bars.Select(x => new Quote
        {
            Open = x.Open,
            High = x.High,
            Low = x.Low,
            Close = x.Close,
            Volume = x.Volume,
            Date = x.DateTimeUtc.DateTime,
        });

        switch (type)
        {
            case IndicatorType.Rsi:
                IEnumerable<RsiResult> rsiResults = quotes.GetRsi(_indicatorWindowValues[type]);
                return rsiResults.Select(x => Convert.ToDecimal(x.Rsi));
            case IndicatorType.Sma:
                IEnumerable<SmaResult> smaResults = quotes.GetSma(_indicatorWindowValues[type]);
                return smaResults.Select(x => Convert.ToDecimal(x.Sma));
        }

        return [];
    }
}
