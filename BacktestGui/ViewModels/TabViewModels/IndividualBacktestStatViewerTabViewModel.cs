using ReactiveUI;

namespace BacktestGui.ViewModels.TabViewModels;

public class IndividualBacktestStatViewerTabViewModel : ReactiveObject
{
    public IndividualBacktestStatViewerTabViewModel(
        IBacktestSelectorViewModel backtestSelectorViewModel,
        TradesViewModel tradesViewModel,
        TradesPnlChartViewModel tradesPnlChartViewModel,
        IndividualBacktestStatsViewModel individualBacktestStatsViewModel,
        MaeToMfeChartViewModel maeToMfeChartViewModel, 
        TradesPlotPerTimeScatterPlotViewModel tradesPlotPerTimeScatterPlotViewModel)
    {
        BacktestSelectorViewModel = backtestSelectorViewModel;
        TradesViewModel = tradesViewModel;
        TradesPnlChartViewModel = tradesPnlChartViewModel;
        IndividualBacktestStatsViewModel = individualBacktestStatsViewModel;
        MaeToMfeChartViewModel = maeToMfeChartViewModel;
        TradesPlotPerTimeScatterPlotViewModel = tradesPlotPerTimeScatterPlotViewModel;
    }

    public IBacktestSelectorViewModel BacktestSelectorViewModel { get; }

    public TradesViewModel TradesViewModel { get; }

    public TradesPnlChartViewModel TradesPnlChartViewModel { get; }

    public IndividualBacktestStatsViewModel IndividualBacktestStatsViewModel { get; }

    public MaeToMfeChartViewModel MaeToMfeChartViewModel { get; }
    
    public TradesPlotPerTimeScatterPlotViewModel TradesPlotPerTimeScatterPlotViewModel { get; }
}