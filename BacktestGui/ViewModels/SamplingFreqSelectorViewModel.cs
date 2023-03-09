using System;
using System.Collections.Generic;
using System.Linq;
using IndicatorServiceShared;
using ReactiveUI.Fody.Helpers;
using TradeServicesSharedDotNet.Models1;

namespace BacktestGui.ViewModels;

public class SamplingFreqSelectorViewModel : ViewModelBase
{
    public IEnumerable<SamplingFreq> SamplingFreqEnums =>
        Enum.GetValues(typeof(SamplingFreq))
            .Cast<SamplingFreq>();

    [Reactive]
    public SamplingFreq SelectedSamplingFreq { get; set; } =
        SamplingFreq.ThirtySeconds;
}