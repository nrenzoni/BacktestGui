using System.Collections.Generic;
using System.Linq;
using CustomShared;
using NodaTime;

namespace BacktestGui.Views.Helpers;

public static class TimeChartHelpers
{
    public static ScottPlotHelper GetScotPlotHelper(this IEnumerable<Instant> times, ScottPlotHelperFactory scottPlotHelperFactory)
    {
        var anyPremarketTrades =
            times.Any(x => x.GetNyLocalTime() < new LocalTime(9, 30) ||
                            x.GetNyLocalTime() >= new LocalTime(16, 0));

        var scotPlotHelperTimeSelection = anyPremarketTrades
            ? ScotPlotHelperTimeSelection.IncludePremarket
            : ScotPlotHelperTimeSelection.RegularTradeHours;
        
        return scottPlotHelperFactory.Create(scotPlotHelperTimeSelection);
    }
}