using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using BacktestGui.Configurations;
using BacktestGui.Services;
using CustomShared;
using DynamicData;
using IndicatorServiceShared;
using NodaTime;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using TradeServicesSharedDotNet.Models1;

namespace BacktestGui.ViewModels.Indicators;

public class ProfitFactorTableViewModel : ViewModelBase
{
    private readonly BackendIndicatorStatService _backendIndicatorStatService;

    public ProfitFactorTableViewModel(
        BackendIndicatorStatService backendIndicatorStatService,
        Globals globals,
        SamplingFreqSelectorViewModel samplingFreqSelectorViewModel,
        IndicatorSelectorViewModelFactory indicatorSelectorViewModelFactory,
        SymbolUniverseSelectorViewModel symbolUniverseSelectorViewModel,
        DateRangeSelectorViewModel dateRangeSelectorViewModel)
    {
        _backendIndicatorStatService = backendIndicatorStatService;
        SamplingFreqSelectorViewModel = samplingFreqSelectorViewModel;
        IndicatorSelectorViewModel = indicatorSelectorViewModelFactory.Create(true);
        SymbolUniverseSelectorViewModel = symbolUniverseSelectorViewModel;
        DateRangeSelectorViewModel = dateRangeSelectorViewModel;

        var allParamsForCalcProfitFactorRocPublished =
            this.WhenAnyValue(
                    x => x.SymbolUniverseSelectorViewModel.SelectedSymbolUniverse,
                    x => x.IndicatorSelectorViewModel.FilteredSelectedIndicatorWithParams,
                    x => x.SamplingFreqSelectorViewModel.SelectedSamplingFreq,
                    x => x.DateRangeSelectorViewModel.SelectedStartLocalDate,
                    x => x.DateRangeSelectorViewModel.SelectedEndLocalDate,
                    x => x.LogReturns)
                .Throttle(globals.ThrottleTime)
                .DistinctUntilChanged()
                .Where(x => x.NoNullMembers())
                .ObserveOn(RxApp.MainThreadScheduler)
                .Publish();

        allParamsForCalcProfitFactorRocPublished
            .Select(x =>
                Observable.FromAsync(async () =>
                    await CalcProfitFactorRoc(
                        x.Item1!,
                        x.Item2!,
                        x.Item3!,
                        x.Item4.Value,
                        x.Item5.Value,
                        x.Item6)))
            .Concat()
            .BindTo(this,
                x => x.ProfitFactorMatrixRows);

        allParamsForCalcProfitFactorRocPublished.Connect();
    }

    [Reactive] public bool LogReturns { get; set; } = true;

    [Reactive] public IEnumerable<ProfitFactorMatrixRow>? ProfitFactorMatrixRows { get; set; }

    public SamplingFreqSelectorViewModel SamplingFreqSelectorViewModel { get; }

    public SymbolUniverseSelectorViewModel SymbolUniverseSelectorViewModel { get; }

    public DateRangeSelectorViewModel DateRangeSelectorViewModel { get; }

    public IndicatorSelectorViewModel IndicatorSelectorViewModel { get; }

    private async Task<List<ProfitFactorMatrixRow>?> CalcProfitFactorRoc(
        string symbolUniverse,
        IndicatorWithParams indicatorWithParams,
        SamplingFreq samplingFreq,
        LocalDate startDate,
        LocalDate endDate,
        bool logReturns)
    {
        var indicator = indicatorWithParams.Indicator;
        var indicatorParams = indicatorWithParams.IndicatorParams;

        var calcProfitFactorRocTask
            = _backendIndicatorStatService.CalcProfitFactorRoc(
                symbolUniverse,
                indicatorWithParams,
                samplingFreq,
                logReturns,
                startDate,
                endDate);

        return await calcProfitFactorRocTask;
    }
}