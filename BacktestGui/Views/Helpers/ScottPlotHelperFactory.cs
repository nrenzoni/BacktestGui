using System;
using CustomShared;
using NodaTime;

namespace BacktestGui.Views.Helpers;

public enum ScotPlotHelperTimeSelection
{
    IncludePremarket,
    RegularTradeHours
}

public class ScottPlotHelperFactory
{
    readonly ScottPlotHelper _scottPlotHelperIncludePremarket;
    readonly ScottPlotHelper _scottPlotHelperRegularTradeHours;

    public ScottPlotHelperFactory(MarketDayChecker marketDayChecker)
    {
        var marketDayChecker1 = marketDayChecker;

        _scottPlotHelperIncludePremarket = new ScottPlotHelper(
            marketDayChecker1,
            new LocalTime(4, 0),
            new LocalTime(20, 0));

        _scottPlotHelperRegularTradeHours = new ScottPlotHelper(
            marketDayChecker1,
            new LocalTime(9, 30),
            new LocalTime(16, 0));
    }

    public ScottPlotHelper Create(
        ScotPlotHelperTimeSelection scotPlotHelperTimeSelection)
    {
        switch (scotPlotHelperTimeSelection)
        {
            case ScotPlotHelperTimeSelection.IncludePremarket:
                return _scottPlotHelperIncludePremarket;
            case ScotPlotHelperTimeSelection.RegularTradeHours:
                return _scottPlotHelperRegularTradeHours;
            default:
                throw new ArgumentOutOfRangeException(nameof(scotPlotHelperTimeSelection), scotPlotHelperTimeSelection,
                    null);
        }
    }
}