using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Dynamic;
using System.Linq;
using System.Reactive.Linq;
using BacktestCSharpShared;
using BacktestGui.Configurations;
using BacktestGui.Models;
using BacktestGui.Services;
using BacktestGui.ViewModels.Misc;
using CustomShared;
using DynamicData;
using NodaTime;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace BacktestGui.ViewModels;

public class TestBacktestGroupSelectorViewModel : ViewModelBase, IBacktestGroupSelectorViewModel
{
    public TestBacktestGroupSelectorViewModel(
        ReadOnlyObservableCollection<BacktestRunEntryMongoWithTrades> backtestsFiltered,
        ReadOnlyObservableCollection<string> selectableParams2,
        IReadOnlyCollection<BacktestRunEntryMongoWithTrades>? backtestsGroup)
    {
        BacktestsFiltered = backtestsFiltered;
        ParametersWithAtLeast1DifferentValue = selectableParams2;
        SelectedBacktestGroupTrades = backtestsGroup;
    }

    public IEnumerable<BacktestGrouping>? BacktestGroupings { get; }
    public Dictionary<BacktestGrouping, List<BacktestRunEntryMongoWithTrades>> BacktestGroupingsWithBacktests { get; }
    public IReadOnlyCollection<BacktestRunEntryMongoWithTrades>? BacktestsFiltered { get; }

    public IReadOnlyCollection<BacktestRunEntryMongoWithTrades>? SelectedBacktestGroupTrades { get; }

    public ReadOnlyObservableCollection<string> ParametersWithAtLeast1DifferentValue { get; }

    public LocalDate? MinBacktestDate { get; }

    public LocalDate? MaxBacktestDate { get; }
}

public class BacktestGroupSelectorViewModel : ViewModelBase, IBacktestGroupSelectorViewModel
{
    public BacktestGroupSelectorViewModel(
        BacktestsConnectable backtestsConnectable, 
        Globals globals)
    {
        var backtestsChangeSetThrottled =
            backtestsConnectable
                .Connect()
                .Throttle(globals.ThrottleTime)
                .Publish();

        backtestsChangeSetThrottled
            .ObserveOn(RxApp.MainThreadScheduler)
            .Bind(out _backtestsNonFiltered)
            .DisposeMany()
            .Subscribe();

        backtestsChangeSetThrottled
            .ToCollection()
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(ResetMinMaxBacktestDates);

        var pub = backtestsChangeSetThrottled
            .ToCollection()
            .Select(CalculateBacktestGroupings)
            .Publish();

        _backtestGroupingsWithBacktests =
            pub
                .ObserveOn(RxApp.MainThreadScheduler)
                .ToProperty(this, nameof(BacktestGroupingsWithBacktests));

        pub
            .CombineLatest(this.WhenAnyValue(x => x.SortDescending))
            .Throttle(globals.ThrottleTime)
            .Select(x => GetBacktestGroupingsOrdered(
                x.First.Keys.ToHashSet(),
                x.Second))
            .ObserveOn(RxApp.MainThreadScheduler)
            .BindTo(this, x => x.BacktestGroupings);

        pub.Connect();

        _backtestsFiltered =
            backtestsChangeSetThrottled
                .Filter(backtest =>
                {
                    var date = backtest.StartTime.GetNyDate();
                    return !(MinBacktestDate <= date && date <= MaxBacktestDate);
                })
                .ToCollection()
                .ObserveOn(RxApp.MainThreadScheduler)
                .ToProperty(this, x => x.BacktestsFiltered);

        this.WhenAnyValue(
                x => x.SelectedBacktestGrouping)
            .WhereNotNull()
            .Throttle(globals.ThrottleTime)
            .Select(x => BacktestGroupingsWithBacktests[x])
            .ObserveOn(RxApp.MainThreadScheduler)
            .BindTo(this, x => x.SelectedBacktestGroupTrades);

        this.WhenAnyValue(
                x => x.SelectedBacktestGroupTrades)
            .WhereNotNull()
            .Throttle(globals.ThrottleTime)
            .SelectMany(x => GetParamNamesWithAtLeast1DiffParamValue(x))
            .ToObservableChangeSet()
            .ObserveOn(RxApp.MainThreadScheduler)
            .Bind(out _parametersWithAtLeast1DifferentValue)
            .DisposeMany()
            .Subscribe();

        backtestsChangeSetThrottled.Connect();
    }

    private IEnumerable<BacktestGrouping> GetBacktestGroupingsOrdered(
        HashSet<BacktestGrouping> backtestGroupings,
        bool descending)
    {
        return descending
            ? backtestGroupings.Reverse()
            : backtestGroupings;
    }

    [Reactive] public LocalDate? MinBacktestDate { get; set; }

    [Reactive] public LocalDate? MaxBacktestDate { get; set; }

    private readonly ReadOnlyObservableCollection<string> _parametersWithAtLeast1DifferentValue;

    [Reactive] public IReadOnlyCollection<BacktestRunEntryMongoWithTrades>? SelectedBacktestGroupTrades { get; set; }

    public ReadOnlyObservableCollection<string> ParametersWithAtLeast1DifferentValue =>
        _parametersWithAtLeast1DifferentValue;

    private readonly ReadOnlyObservableCollection<BacktestRunEntryMongoWithTrades> _backtestsNonFiltered;

    public ReadOnlyObservableCollection<BacktestRunEntryMongoWithTrades>? BacktestsNonFiltered => _backtestsNonFiltered;

    private readonly ObservableAsPropertyHelper<IReadOnlyCollection<BacktestRunEntryMongoWithTrades>?>
        _backtestsFiltered;

    private readonly ObservableAsPropertyHelper<Dictionary<BacktestGrouping, List<BacktestRunEntryMongoWithTrades>>>
        _backtestGroupingsWithBacktests;

    public Dictionary<BacktestGrouping, List<BacktestRunEntryMongoWithTrades>> BacktestGroupingsWithBacktests =>
        _backtestGroupingsWithBacktests.Value;

    [Reactive] public IEnumerable<BacktestGrouping>? BacktestGroupings { get; set; }

    [Reactive] public BacktestGrouping? SelectedBacktestGrouping { get; set; }

    public IReadOnlyCollection<BacktestRunEntryMongoWithTrades>? BacktestsFiltered => _backtestsFiltered.Value;

    [Reactive] public bool SortDescending { get; set; } = true;

    private void ResetMinMaxBacktestDates(
        IReadOnlyCollection<BacktestRunEntryMongoWithTrades>? backtestRunEntryMongoWithTradesCollection)
    {
        if (backtestRunEntryMongoWithTradesCollection is null)
        {
            MinBacktestDate = null;
            MaxBacktestDate = null;
            return;
        }

        LocalDate minDate = LocalDate.MaxIsoValue;
        LocalDate maxDate = LocalDate.MinIsoValue;

        foreach (var backtestRunEntryMongoWithTrades in backtestRunEntryMongoWithTradesCollection)
        {
            minDate = LocalDate.Min(minDate, backtestRunEntryMongoWithTrades.StartTime.GetNyDate());
            maxDate = LocalDate.Max(maxDate, backtestRunEntryMongoWithTrades.StartTime.GetNyDate());
        }

        MinBacktestDate = minDate;
        MaxBacktestDate = maxDate;
    }

    private static HashSet<string>? GetParamNamesWithAtLeast1DiffParamValue(
        IReadOnlyCollection<BacktestRunEntryMongoWithTrades>? backtests)
    {
        if (backtests is null)
            return null;

        DefaultableDictionary<string, HashSet<object>> paramsWithValues
            = new(() => new());

        foreach (var backtestRunEntryMongoWithTrades in backtests)
        {
            foreach (var (_, paramMap) in backtestRunEntryMongoWithTrades.StrategyParameterDictionaries)
            {
                if (paramMap is not ExpandoObject x)
                    continue;

                foreach (var (paramName, paramVal) in x)
                {
                    if (paramVal is null)
                        continue;

                    paramsWithValues[paramName]
                        .Add(paramVal);
                }
            }
        }

        return paramsWithValues
            .Where(x => x.Value.Count > 1)
            .Select(x => x.Key)
            .ToHashSet();
    }

    private Dictionary<BacktestGrouping, List<BacktestRunEntryMongoWithTrades>>
        CalculateBacktestGroupings(
            IReadOnlyCollection<BacktestRunEntryMongoWithTrades>? backtestRunEntryMongoWithTradesCollection)
    {
        if (backtestRunEntryMongoWithTradesCollection is null)
        {
            return new();
        }

        var backtestGroupingsDic
            = new DefaultableDictionary<int, List<BacktestRunEntryMongoWithTrades>>(() => new());

        foreach (var backtestRunEntryMongoWithTrades in backtestRunEntryMongoWithTradesCollection)
        {
            if (backtestRunEntryMongoWithTrades.GroupingNumber is null)
                continue;

            backtestGroupingsDic[backtestRunEntryMongoWithTrades.GroupingNumber.Value]
                .Add(backtestRunEntryMongoWithTrades);
        }

        var backtestGroupings
            = new Dictionary<BacktestGrouping, List<BacktestRunEntryMongoWithTrades>>(
                backtestGroupingsDic.Count);

        foreach (var (groupingNum, currBacktests) in backtestGroupingsDic)
        {
            var currBacktestsParamCountDic
                = new DefaultableDictionary<string, HashSet<object>>(() => new());

            foreach (var backtest in currBacktests)
            {
                foreach (var (currStrategy, currBacktestCurrParamValDict)
                         in backtest.StrategyParameterDictionaries)
                {
                    if (currBacktestCurrParamValDict is not ExpandoObject paramMap)
                        continue;

                    foreach (var (paramKey, paramVal) in paramMap)
                    {
                        currBacktestsParamCountDic[paramKey]
                            .Add(paramVal);
                    }
                }
            }

            List<string> selectableParameters = new();

            foreach (var (currParamKey, currParamUniqVals) in currBacktestsParamCountDic)
            {
                if (currParamUniqVals.Count > 1)
                {
                    selectableParameters.Add(currParamKey);
                }
            }

            var grouping = new BacktestGrouping(
                groupingNum,
                currBacktests.First().Strategies,
                selectableParameters);

            backtestGroupings[grouping] = currBacktests;
        }

        return backtestGroupings;
    }
}