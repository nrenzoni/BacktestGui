using System.Collections.Generic;
using System.Reactive.Linq;
using BacktestCSharpShared;
using BacktestGui.Configurations;
using BacktestGui.Services;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace BacktestGui.ViewModels;

public class TradesPlotPerTimeScatterPlotViewModel : ViewModelBase
{
    public TradesPlotPerTimeScatterPlotViewModel(IBacktestSelectorViewModel backtestSelectorViewModel,
        Globals globals)
    {
        backtestSelectorViewModel
            .WhenAnyValue(x => x.SelectedBacktest)
            .Throttle(globals.ThrottleTime)
            .Select(x => x?.TradeRecords)
            .BindTo(this, x => x.Trades);
    }

    [Reactive] public List<TradeRecord>? Trades { get; set; }
}