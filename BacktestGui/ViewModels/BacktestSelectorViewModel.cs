using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Windows.Input;
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

public interface IBacktestSelectorViewModel
{
    public BacktestVmModel? SelectedBacktest { get; set; }

    public int? SelectedBacktestGroupingNumber { get; set; }

    public HashSet<int?> SelectableBacktestGroupingNumbers { get; set; }

    public LocalDate? MinDate { get; }

    public LocalDate? MaxDate { get; }
}

public class BacktestSelectorViewModel : ViewModelBase,
    IBacktestSelectorViewModel
{
    public BacktestSelectorViewModel(
        BacktestsConnectable backtestsConnectable, 
        Converters converters,
        Globals globals)
    {
        ClearFiltersCmd = ReactiveCommand.Create(ClearFilters);

        var backtestHolderThrottled =
            backtestsConnectable
                .Connect()
                .Throttle(globals.ThrottleTime)
                .Transform(converters.ToBacktestVmModel)
                .Publish();

        _backtests = backtestHolderThrottled
            .ToCollection()
            .ObserveOn(RxApp.MainThreadScheduler)
            .Do(x => { })
            .ToProperty(this, x => x.Backtests);

        _filteredSelectableBacktests
            = this.WhenAnyValue(x => x.SelectedBacktestGroupingNumber,
                    x => x.SortDescending)
                .CombineLatest(backtestHolderThrottled.ToCollection())
                .Throttle(globals.ThrottleTime)
                .Select(x => FilterAndSortBacktests(
                    x.First.Item1,
                    x.First.Item2,
                    x.Second))
                .ToProperty(this, nameof(FilteredSelectableBacktests));

        var backtestHolderAsCollectionPublished = backtestHolderThrottled
            .ToCollection()
            .Publish();

        var minMaxDates
            = backtestHolderAsCollectionPublished
                .Select(x =>
                {
                    var dates = x.Select(y => y.StartTime.GetNyDate()).ToArray();
                    LocalDate minDate = LocalDate.MaxIsoValue, maxDate = LocalDate.MinIsoValue;
                    foreach (var date in dates)
                    {
                        minDate = LocalDate.Min(minDate, date);
                        maxDate = LocalDate.Max(maxDate, date);
                    }

                    return (minDate, maxDate);
                })
                .Publish();

        minMaxDates
            .Select(x => x.minDate)
            .ObserveOn(RxApp.MainThreadScheduler)
            .Do(x => _minDateReset = x)
            .BindTo(this, x => x.MinDate);

        minMaxDates
            .Select(x => x.maxDate)
            .ObserveOn(RxApp.MainThreadScheduler)
            .Do(x => _maxDateReset = x)
            .BindTo(this, x => x.MaxDate);

        minMaxDates.Connect();

        backtestHolderAsCollectionPublished.Connect();

        backtestHolderAsCollectionPublished
            .Select(x => x.Select(y => y.GroupingNumber).ToHashSet())
            .BindTo(this, x => x.SelectableBacktestGroupingNumbers);

        backtestHolderThrottled.Connect();
    }

    [Reactive] public HashSet<int?> SelectableBacktestGroupingNumbers { get; set; }

    [Reactive] public int? SelectedBacktestGroupingNumber { get; set; }

    private LocalDate? _minDateReset;

    [Reactive] public LocalDate? MinDate { get; set; }

    private LocalDate? _maxDateReset;

    [Reactive] public LocalDate? MaxDate { get; set; }

    private readonly ObservableAsPropertyHelper<IReadOnlyCollection<BacktestVmModel>>
        _filteredSelectableBacktests;

    private readonly ObservableAsPropertyHelper<IReadOnlyCollection<BacktestVmModel>?> _backtests;

    public IReadOnlyCollection<BacktestVmModel> FilteredSelectableBacktests =>
        _filteredSelectableBacktests.Value;

    public IReadOnlyCollection<BacktestVmModel>? Backtests => _backtests.Value;

    public ICommand ClearFiltersCmd { get; }

    private void ClearFilters()
    {
        SelectedBacktestGroupingNumber = null;
        MinDate = _minDateReset;
        MaxDate = _maxDateReset;
    }

    private IReadOnlyCollection<BacktestVmModel> FilterAndSortBacktests(
        int? groupNumber,
        bool sortDescending,
        IReadOnlyCollection<BacktestVmModel> backtests)
    {
        var filtered
            = FilterBacktests(groupNumber, backtests);

        if (sortDescending)
        {
            filtered.Reverse();
        }

        return filtered;
    }
    
    private List<BacktestVmModel> FilterBacktests(int? groupNumber, IReadOnlyCollection<BacktestVmModel> backtests)
    {
        if (groupNumber is null)
            return backtests.ToList();

        List<BacktestVmModel> filteredSelectableBacktests = new();

        foreach (var backtest in backtests)
        {
            if (groupNumber is not null)
            {
                if (backtest.GroupingNumber != groupNumber)
                    continue;
            }

            filteredSelectableBacktests.Add(
                backtest);
        }


        return filteredSelectableBacktests;
    }

    [Reactive] public BacktestVmModel? SelectedBacktest { get; set; }

    [Reactive] public bool SortDescending { get; set; } = true;
}