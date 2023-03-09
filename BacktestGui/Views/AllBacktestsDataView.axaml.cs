using System;
using System.Reactive.Linq;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using BacktestGui.Models;
using BacktestGui.ViewModels;

namespace BacktestGui.Views;

public partial class AllBacktestsDataView :
        UserControl
    // ReactiveUserControl<AllBacktestsDataViewModel>
{
    private AllBacktestsDataViewModel AllBacktestsDataViewModel
        => (AllBacktestsDataViewModel)DataContext;

    public AllBacktestsDataView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        // this.WhenActivated(disposable => { });

        AvaloniaXamlLoader.Load(this);
    }

    private DataGrid DataGrid1 => this.Find<DataGrid>("AllBacktestsDataGrid");

    private async void OnDataGridSelectionChanged(object? sender, EventArgs eventArgs)
    {
        if (!Equals(sender, DataGrid1))
            throw new Exception($"{nameof(sender)} must be {nameof(DataGrid1)}.");

        var selectedItem = DataGrid1.SelectedItem;

        if (selectedItem is not null and not BacktestVmModel)
            throw new Exception();

        await AllBacktestsDataViewModel.OnDataGridRowSelectionChanged.Execute(
            (BacktestVmModel?)selectedItem);
    }
}