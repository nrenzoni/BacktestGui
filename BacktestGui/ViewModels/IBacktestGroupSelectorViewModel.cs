using System.Collections.Generic;
using System.Collections.ObjectModel;
using BacktestCSharpShared;
using BacktestGui.Models;
using NodaTime;

namespace BacktestGui.ViewModels;

public interface IBacktestGroupSelectorViewModel
{
    public IEnumerable<BacktestGrouping>? BacktestGroupings { get; }

    public Dictionary<BacktestGrouping, List<BacktestRunEntryMongoWithTrades>>
        BacktestGroupingsWithBacktests { get; }

    // public IReadOnlyCollection<BacktestRunEntryMongoWithTrades>? BacktestsFiltered { get; }

    public IReadOnlyCollection<BacktestRunEntryMongoWithTrades>? SelectedBacktestGroupTrades { get; }

    public ReadOnlyObservableCollection<string> ParametersWithAtLeast1DifferentValue { get; }

    public LocalDate? MinBacktestDate { get; }

    public LocalDate? MaxBacktestDate { get; }
}