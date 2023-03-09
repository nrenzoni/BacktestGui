using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Windows.Input;
using BacktestGui.Configurations;
using BacktestGui.Models;
using BacktestGui.Services;
using BacktestGui.ViewModels.Misc;
using DynamicData;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace BacktestGui.ViewModels;

public class AllBacktestsDataViewModel : ViewModelBase
{
    public static uint? ParseOrNull(string? str, bool zeroIsNull)
    {
        var isInt = UInt32.TryParse(str, out var res);
        if (!isInt)
            return null;
        if (res == 0 & zeroIsNull)
            return null;
        return res;
    }

    public AllBacktestsDataViewModel(
        BacktestsConnectable backtestsConnectable,
        Globals globals,
        Converters converters,
        IBacktestSelectorViewModel backtestSelectorViewModel)
    {
        ResetFilters = ReactiveCommand.Create(ResetFiltersAsync);
        OnDataGridRowSelectionChanged = ReactiveCommand.Create((BacktestVmModel? backtestVmModel) =>
        {
            backtestSelectorViewModel.SelectedBacktest = backtestVmModel;
        });

        var minOrderObs =
            this.WhenAnyValue(
                    x => x.MinOrderFills)
                .Throttle(globals.ThrottleTime)
                .Select(x => ParseOrNull(x, true))
                .DistinctUntilChanged();

        var sortSelectedObs =
            this.WhenAnyValue(x => x.SortDescending)
                .Throttle(globals.ThrottleTime);

        var groupNumberObs =
            this.WhenAnyValue(x => x.GroupNumber)
                .Throttle(globals.ThrottleTime)
                .Select(x => ParseOrNull(x, false))
                .DistinctUntilChanged();

        _filteredBacktests
            = backtestsConnectable
                .Connect()
                .Throttle(globals.ThrottleTime)
                .Transform(converters.ToBacktestVmModel)
                .ToCollection()
                .CombineLatest(minOrderObs, sortSelectedObs, groupNumberObs)
                .Select(x => Filter(x))
                .ObserveOn(RxApp.MainThreadScheduler)
                .ToProperty(this, nameof(FilteredBacktests));
    }

    private readonly ObservableAsPropertyHelper<IEnumerable<BacktestVmModel>?> _filteredBacktests;

    public IEnumerable<BacktestVmModel>? FilteredBacktests => _filteredBacktests.Value;

    private IEnumerable<BacktestVmModel>? Filter(
        (IReadOnlyCollection<BacktestVmModel> First, uint? Second, bool Third, uint? Fourth) valueTuple)
    {
        var (allBacktests, minOrderFills, sortDescending, groupNumber) = valueTuple;

        List<BacktestVmModel> filteredOrderFills;

        if (groupNumber is not null)
        {
            filteredOrderFills = allBacktests.Where(x => x.GroupingNumber == groupNumber).ToList();
        }
        else
            filteredOrderFills = allBacktests.ToList();

        if (minOrderFills is not null)
        {
            filteredOrderFills =
                filteredOrderFills
                    .Where(x => x.TotalOrderFills >= minOrderFills)
                    .ToList();
        }

        if (sortDescending)
        {
            filteredOrderFills.Reverse();
        }

        return filteredOrderFills;
    }

    [Reactive] public string? MinOrderFills { get; set; }

    public ICommand ResetFilters { get; }

    private void ResetFiltersAsync()
    {
        MinOrderFills = null;
    }

    [Reactive] public bool SortDescending { get; set; } = true;

    [Reactive] public string? GroupNumber { get; set; }

    public ReactiveCommand<BacktestVmModel?, Unit> OnDataGridRowSelectionChanged { get; }
}