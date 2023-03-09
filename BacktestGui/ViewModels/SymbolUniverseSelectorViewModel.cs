using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using BacktestGui.Configurations;
using BacktestGui.Services;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace BacktestGui.ViewModels;

public class SymbolUniverseSelectorViewModel : ViewModelBase
{
    public SymbolUniverseSelectorViewModel(BackendIndicatorStatService backendIndicatorStatService,
        Globals globals)
    {
        _selectableSymbolUniverses = backendIndicatorStatService.SelectableIndicatorMetaDataConnectableObservable
            .Select(x => x.SymbolUniverses)
            .ToProperty(this, nameof(SelectableSymbolUniverses));

        this.WhenAnyValue(x => x.SelectableSymbolUniverses)
            .Throttle(globals.ThrottleTime)
            .Select(x => x?.First())
            .BindTo(this, x => x.SelectedSymbolUniverse);
    }

    private readonly ObservableAsPropertyHelper<List<string>> _selectableSymbolUniverses;

    public IList<string> SelectableSymbolUniverses => _selectableSymbolUniverses.Value;

    [Reactive] public string? SelectedSymbolUniverse { get; set; }
}