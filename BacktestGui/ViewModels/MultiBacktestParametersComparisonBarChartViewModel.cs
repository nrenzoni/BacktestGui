using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Dynamic;
using System.Linq;
using System.Reactive.Linq;
using BacktestCSharpShared;
using BacktestGui.Models;
using BacktestGui.Services;
using BacktestGui.Utils;
using CustomShared;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace BacktestGui.ViewModels;

public interface IMultiBacktestParametersComparisonBarChartViewModel
{
}

public class MultiBacktestParametersComparisonBarChartViewModel : ViewModelBase,
    IMultiBacktestParametersComparisonBarChartViewModel,
    IActivatableViewModel
{
    public MultiBacktestParametersComparisonBarChartViewModel(
        IBacktestSelectorViewModel backtestSelectorViewModel,
        IBacktestGroupSelectorViewModel backtestGroupSelectorViewModel)
    {
        BacktestSelectorViewModel = backtestSelectorViewModel;
        BacktestGroupSelectorViewModel = backtestGroupSelectorViewModel;

        _selectableParameters =
            BacktestGroupSelectorViewModel
                .WhenAnyValue(
                    x => x.ParametersWithAtLeast1DifferentValue)
                .ToProperty(this, nameof(SelectableParameters));

        BacktestGroupSelectorViewModel
            .WhenAnyValue(
                x => x.SelectedBacktestGroupTrades)
            .BindTo(this, x => x.SelectedBacktestGroupTrades);

        _paramHolderRecords = this
            .WhenAnyValue(
                x => x.SelectedBacktestGroupTrades,
                x => x.SelectedComparisonParam1)
            .Throttle(TimeSpan.FromSeconds(0.5))
            .ObserveOn(RxApp.TaskpoolScheduler)
            .Select(x =>
                UpdateParamHolderRecords(x.Item2, x.Item1))
            .ObserveOn(RxApp.MainThreadScheduler)
            .ToProperty(this, nameof(ParamHolderRecords));
    }

    private readonly ObservableAsPropertyHelper<ReadOnlyObservableCollection<string>>? _selectableParameters;

    public ReadOnlyObservableCollection<string>? SelectableParameters => _selectableParameters?.Value;

    public IBacktestGroupSelectorViewModel BacktestGroupSelectorViewModel { get; }

    public IBacktestSelectorViewModel BacktestSelectorViewModel { get; }

    [Reactive] public IReadOnlyCollection<BacktestRunEntryMongoWithTrades>? SelectedBacktestGroupTrades { get; set; }

    [Reactive] public string? SelectedComparisonParam1 { get; set; }

    public IEnumerable<YAxisPlotType> YAxisPlotTypeEnums =>
        Enum.GetValues(typeof(YAxisPlotType))
            .Cast<YAxisPlotType>();

    [Reactive] public YAxisPlotType YAxisPlotType { get; set; }

    private readonly ObservableAsPropertyHelper<ObservableCollection<ParamHolderRecord>?> _paramHolderRecords;

    public ObservableCollection<ParamHolderRecord>? ParamHolderRecords => _paramHolderRecords.Value;

    private ObservableCollection<ParamHolderRecord>? UpdateParamHolderRecords(
        string? comparisonParam1,
        IReadOnlyCollection<BacktestRunEntryMongoWithTrades>? selectedGroupingBacktests)
    {
        if (comparisonParam1 is null || selectedGroupingBacktests is null)
            return null;

        DefaultableDictionary<object, List<BacktestRunEntryMongoWithTrades>> backtestsByCompareParam1 =
            new(() => new());

        foreach (var backtest in selectedGroupingBacktests)
        {
            foreach (var (stratName, stratParamMap) in backtest.StrategyParameterDictionaries)
            {
                if (stratParamMap is not ExpandoObject e)
                    throw new Exception();

                IDictionary<string, object?> paramDict = e;

                var paramKeyFound = paramDict.TryGetValue(
                    comparisonParam1,
                    out var currParamVal);

                if (!paramKeyFound || currParamVal is null)
                    continue;

                var currParamValAsDecimal = Convert.ToDecimal(currParamVal);

                backtestsByCompareParam1[currParamValAsDecimal].Add(backtest);
            }
        }

        ObservableCollection<ParamHolderRecord> paramHolderRecords = new();

        foreach (var (paramVal, currBacktests) in backtestsByCompareParam1)
        {
            var valueDictionary = new Dictionary<YAxisPlotType, decimal>();

            var backtestGroupCumProfitAvg = currBacktests
                .Select(TradeCalculators.CalcProfitReturnForBacktest)
                .Average();
            valueDictionary[YAxisPlotType.CumProfit] = backtestGroupCumProfitAvg;

            var tradesPerDayAvgAvg = currBacktests.Select(TradeCalculators.TradesPerDayAvg).Average();
            valueDictionary[YAxisPlotType.TradesPerDayAvg] = Convert.ToDecimal(tradesPerDayAvgAvg);

            var maxDdPerBacktest
                = currBacktests.Select(BacktestMetricCalculator.MaxDrawdown).ToArray();

            decimal maxDdDollarVal = decimal.MinValue, maxDdPct = 0M;

            foreach (var maxDdTuple in maxDdPerBacktest)
            {
                if (maxDdTuple is null)
                    continue;

                maxDdDollarVal = Math.Max(maxDdDollarVal, maxDdTuple.Value.maxDd);
                maxDdPct = Math.Max(maxDdPct, maxDdTuple.Value.maxDdPct);
            }

            valueDictionary[YAxisPlotType.MaxDrawdownDollarVal] = maxDdDollarVal;
            valueDictionary[YAxisPlotType.MaxDrawdownPct] = maxDdPct;

            var paramHolderRecord = new ParamHolderRecord(
                Convert.ToDouble(paramVal),
                valueDictionary
            );

            paramHolderRecords.Add(paramHolderRecord);
        }

        return paramHolderRecords;
    }

    public ViewModelActivator Activator { get; } = new();
}