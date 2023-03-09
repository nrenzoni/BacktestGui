using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Selection;
using Avalonia.Data;
using Avalonia.Markup.Xaml;
using BacktestGui.Configurations;
using BacktestGui.Utils;
using BacktestGui.ViewModels.Indicators;
using DynamicData.Binding;
using ReactiveUI;

namespace BacktestGui.Views.Indicators;

public partial class MultiIndicatorTableView : UserControl
{
    private readonly Globals _globals;

    private MultiIndicatorTableViewModel MultiIndicatorTableViewModel =>
        (MultiIndicatorTableViewModel)DataContext;

    private ListBox IndicatorStatColumnsListBox1 => this.Get<ListBox>("IndicatorStatColumnsListBox");


    private DataGrid IndicatorsDataGrid1
        => this.GetControl<DataGrid>("IndicatorsDataGrid");

    public MultiIndicatorTableView()
    {
        _globals = Bootstrapper.GetService<Globals>();

        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        if (MultiIndicatorTableViewModel is null)
            return;

        MultiIndicatorTableViewModel.WhenPropertyChanged(
                x => x.IndicatorStatColumnsSelectorViewModel.SelectedFields)
            .Throttle(_globals.ThrottleTime)
            .Select(x => x.Value?.ToList())
            .Where(x => x?.Any() is true)
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(x => UpdateIndicatorsDataGrid(x));

        // IndicatorStatColumnsListBox1.Selection.Select(0);
        // IndicatorStatColumnsListBox1.Selection.Select(1);
    }

    private void UpdateIndicatorsDataGrid(IList<string> indicatorColumnsToShow)
    {
        IndicatorsDataGrid1.Columns.Clear();

        var dataGridTextColumn = new DataGridTextColumn
        {
            Header = "Indicator",
            Binding = new Binding(nameof(IndicatorSummaryStatsForView.IndicatorStr))
        };

        IndicatorsDataGrid1.Columns.Add(
            dataGridTextColumn);


        foreach (var indicatorColumnName in indicatorColumnsToShow)
        {
            dataGridTextColumn = new DataGridTextColumn
            {
                Header = indicatorColumnName,
                Binding = new Binding($"{indicatorColumnName}")
            };

            IndicatorsDataGrid1.Columns.Add(
                dataGridTextColumn);
        }
    }
}