using Proton.Engine.Core.Models;

namespace Proton.Engine.Core.Interfaces;

public interface IIndicatorService
{
    Dictionary<IndicatorType, int> IndicatorWindowValues { get; }

    IEnumerable<decimal> CalculateIndicator(IndicatorType type, IEnumerable<Bar> bars);
}
