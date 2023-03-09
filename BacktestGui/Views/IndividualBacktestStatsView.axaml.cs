using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using Avalonia.Controls;
using Avalonia.Controls.Mixins;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using BacktestGui.ViewModels;
using ReactiveUI;

namespace BacktestGui.Views;

public partial class IndividualBacktestStatsView : ReactiveUserControl<IndividualBacktestStatsViewModel>
{
    public IndividualBacktestStatsView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        this.WhenActivated(disposable =>
        {
            ViewModel?.WhenAnyValue(x => x.ShowLongStats)
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(x => ChangeLongStatColumnsVisibility(x))
                .DisposeWith(disposable);

            ViewModel?.WhenAnyValue(x => x.ShowShortStats)
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(x => ChangeShortStatColumnsVisibility(x))
                .DisposeWith(disposable);
        });

        AvaloniaXamlLoader.Load(this);
    }

    private void ChangeLongStatColumnsVisibility(bool visible)
    {
        var dataGridWidth = visible
            ? new DataGridLength(0, DataGridLengthUnitType.Auto)
            : new DataGridLength(0);

        foreach (var longStatsColumn in LongStatsColumns)
        {
            longStatsColumn.Width = dataGridWidth;
        }
    }

    private void ChangeShortStatColumnsVisibility(bool visible)
    {
        var dataGridWidth = visible
            ? new DataGridLength(0, DataGridLengthUnitType.Auto)
            : new DataGridLength(0);

        foreach (var longStatsColumn in ShortStatsColumns)
        {
            longStatsColumn.Width = dataGridWidth;
        }
    }

    public DataGrid IndividualBacktestStatDataGridCode => this.FindControl<DataGrid>("IndividualBacktestStatDataGrid");

    public IEnumerable<DataGridColumn> LongStatsColumns => IndividualBacktestStatDataGridCode.Columns.ToList()
        .Where(x => x.Header.ToString().ToLower().Contains("long"));

    public IEnumerable<DataGridColumn> ShortStatsColumns => IndividualBacktestStatDataGridCode.Columns.ToList()
        .Where(x => x.Header.ToString().ToLower().Contains("short"));
}