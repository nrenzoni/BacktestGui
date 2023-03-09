using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Avalonia.Controls.Selection;
using BacktestGui.Configurations;
using BacktestGui.Services;
using BacktestGui.ViewModels.Helpers;
using BacktestGui.ViewModels.Shared;
using CsvHelper.TypeConversion;
using CustomShared;
using IndicatorProto;
using IndicatorServiceShared;
using NodaTime;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using IndicatorWithParams = IndicatorServiceShared.IndicatorWithParams;
using SamplingFreq = TradeServicesSharedDotNet.Models1.SamplingFreq;

namespace BacktestGui.ViewModels.Indicators;

public enum IndicatorSummaryStatColumn
{
    RangeToIqrRatio,
    RelativeEntropy,
    MinValue,
    MedianValue,
    MaxValue
}

public record IndicatorSummaryStatsForView(
    string IndicatorName,
    string IndicatorStr,
    Dictionary<string, decimal> IndicatorParams,
    decimal? RangeToIqrRatio,
    decimal? RelativeEntropy,
    decimal MinValue,
    decimal Percentile25,
    decimal MedianValue,
    decimal Percentile75,
    decimal MaxValue)
{
}

public class MultiIndicatorTableViewModel : ViewModelBase
{
    
    private readonly BackendIndicatorStatService _backendIndicatorStatService;

    public MultiIndicatorTableViewModel(
        BackendIndicatorStatService backendIndicatorStatService,
        SymbolUniverseSelectorViewModel symbolUniverseSelectorViewModel,
        DateRangeSelectorViewModel dateRangeSelectorViewModel,
        Globals globals, SamplingFreqSelectorViewModel samplingFreqSelectorViewModel,
        IndicatorSelectorViewModelFactory indicatorSelectorViewModelFactory,
        MultiFieldSelectorViewModelFactory multiFieldSelectorViewModelFactory)
    {
        _backendIndicatorStatService = backendIndicatorStatService;
        SymbolUniverseSelectorViewModel = symbolUniverseSelectorViewModel;
        DateRangeSelectorViewModel = dateRangeSelectorViewModel;
        SamplingFreqSelectorViewModel = samplingFreqSelectorViewModel;
        IndicatorSelectorViewModel = indicatorSelectorViewModelFactory.Create(false);
        IndicatorStatColumnsSelectorViewModel =
            multiFieldSelectorViewModelFactory.Create(
                false,
                true,
                IndicatorStatColumns);

        _indicatorSummaryStats = this.WhenAnyValue(
                x => x.SymbolUniverseSelectorViewModel.SelectedSymbolUniverse,
                x => x.IndicatorSelectorViewModel.FilteredSelectedIndicatorsWithParams,
                x => x.SamplingFreqSelectorViewModel.SelectedSamplingFreq,
                x => x.DateRangeSelectorViewModel.SelectedStartLocalDate,
                x => x.DateRangeSelectorViewModel.SelectedEndLocalDate)
            .Throttle(globals.ThrottleTime)
            .Where(x =>
                x.NoNullMembers() && x.Item2!.Any())
            .Select(x =>
                Observable.FromAsync(async () => await CalcSummaryStats(
                    x.Item1,
                    x.Item2,
                    x.Item3,
                    x.Item4.Value,
                    x.Item5.Value)))
            .Merge(1)
            .ToProperty(this, nameof(IndicatorSummaryStats));
    }

    public static List<string> IndicatorStatColumns =>
        EnumExtensions.GetEnumTypesAsStrList<IndicatorSummaryStatColumn>();

    private async Task<List<IndicatorSummaryStatsForView>> CalcSummaryStats(
        string symbolUniverse,
        List<IndicatorWithParams> indicatorWithParamsList,
        SamplingFreq samplingFreq,
        LocalDate startDate,
        LocalDate endDate)
    {
        var indicatorSummaryStatsResponse =
            await _backendIndicatorStatService.CalcSummaryStats(
                symbolUniverse,
                indicatorWithParamsList,
                startDate,
                endDate,
                samplingFreq);

        var indicatorSummaryStatsForViewList
            = new List<IndicatorSummaryStatsForView>();

        foreach (var (indicator, stats) in indicatorSummaryStatsResponse.IndicatorSummaryStatsMap)
        {
            var indicatorSummaryStatsForView = new IndicatorSummaryStatsForView(
                indicator.Indicator,
                indicator.ToString(),
                indicator.IndicatorParams,
                stats.RangeToIqrRatio,
                stats.RelativeEntropy,
                stats.Percentile0,
                0,
                stats.Percentile50,
                0,
                stats.Percentile100);
            indicatorSummaryStatsForViewList.Add(indicatorSummaryStatsForView);
        }

        return indicatorSummaryStatsForViewList;
    }

    private readonly ObservableAsPropertyHelper<List<IndicatorSummaryStatsForView>> _indicatorSummaryStats;

    public List<IndicatorSummaryStatsForView> IndicatorSummaryStats => _indicatorSummaryStats.Value;

    public SymbolUniverseSelectorViewModel SymbolUniverseSelectorViewModel { get; }

    public DateRangeSelectorViewModel DateRangeSelectorViewModel { get; }

    public IndicatorSelectorViewModel IndicatorSelectorViewModel { get; }

    public MultiFieldSelectorViewModel IndicatorStatColumnsSelectorViewModel { get; }

    public SamplingFreqSelectorViewModel SamplingFreqSelectorViewModel { get; }
}