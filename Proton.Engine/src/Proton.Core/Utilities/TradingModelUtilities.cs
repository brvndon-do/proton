using Proton.Engine.Core.Models.Trading;

namespace Proton.Engine.Core.Utilities;

public static class TradingModelUtilities
{
    public static TimeFrame ConvertToTimeFrame(this string str) => str.Trim().ToLower() switch
    {
        "daily" => TimeFrame.Daily,
        "hourly" => TimeFrame.Hourly,
        _ => TimeFrame.Daily
    };
}
