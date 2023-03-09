using BacktestGui.ViewModels.Indicators;
using ReactiveUI;

namespace BacktestGui.ViewModels;

public class IndicatorResearchTabViewModel
    : ReactiveObject
{
    public IndicatorResearchTabViewModel(
        ProfitFactorTableViewModel profitFactorTableViewModel,
        MultiIndicatorTableViewModel multiIndicatorTableViewModel)
    {
        ProfitFactorTableViewModel = profitFactorTableViewModel;
        MultiIndicatorTableViewModel = multiIndicatorTableViewModel;
    }

    public ProfitFactorTableViewModel ProfitFactorTableViewModel { get; set; }

    public MultiIndicatorTableViewModel MultiIndicatorTableViewModel { get; set; }
}