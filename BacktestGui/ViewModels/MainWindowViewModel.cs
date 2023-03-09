using BacktestGui.ViewModels.TabViewModels;

namespace BacktestGui.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {
        public MainWindowViewModel(
            MultiBacktestParametersComparisonBarChartViewModel multiBacktestParametersComparisonBarChartViewModel,
            IBacktestSelectorViewModel backtestSelectorViewModel,
            IBacktestGroupSelectorViewModel backtestGroupSelectorViewModel,
            IndividualBacktestStatsViewModel individualBacktestStatsViewModel,
            TradesPnlChartViewModel tradesPnlChartViewModel,
            TradesViewModel tradesViewModel,
            AllBacktestsDataViewModel allBacktestsDataViewModel,
            IndividualBacktestStatViewerTabViewModel individualBacktestStatViewerTabViewModel,
            IndicatorResearchTabViewModel indicatorResearchTabViewModel)
        {
            MultiBacktestParametersComparisonBarChartViewModel = multiBacktestParametersComparisonBarChartViewModel;
            BacktestSelectorViewModel = backtestSelectorViewModel;
            BacktestGroupSelectorViewModel = backtestGroupSelectorViewModel;
            IndividualBacktestStatsViewModel = individualBacktestStatsViewModel;
            TradesPnlChartViewModel = tradesPnlChartViewModel;
            TradesViewModel = tradesViewModel;
            AllBacktestsDataViewModel = allBacktestsDataViewModel;
            IndividualBacktestStatViewerTabViewModel = individualBacktestStatViewerTabViewModel;
            IndicatorResearchTabViewModel = indicatorResearchTabViewModel;
        }

        public MultiBacktestParametersComparisonBarChartViewModel
            MultiBacktestParametersComparisonBarChartViewModel { get; }

        public IBacktestSelectorViewModel BacktestSelectorViewModel { get; }

        public IBacktestGroupSelectorViewModel BacktestGroupSelectorViewModel { get; }

        public IndividualBacktestStatsViewModel IndividualBacktestStatsViewModel { get; }

        public TradesPnlChartViewModel TradesPnlChartViewModel { get; }

        public TradesViewModel TradesViewModel { get; }

        public AllBacktestsDataViewModel AllBacktestsDataViewModel { get; }

        public IndividualBacktestStatViewerTabViewModel IndividualBacktestStatViewerTabViewModel { get; }

        public IndicatorResearchTabViewModel IndicatorResearchTabViewModel { get; }
    }
}