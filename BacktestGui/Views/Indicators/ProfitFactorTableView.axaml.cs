using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using BacktestGui.ViewModels.Indicators;
using DynamicData;
using DynamicData.Binding;
using ReactiveUI.Fody.Helpers;
using Console = System.Console;

namespace BacktestGui.Views.Indicators;

public partial class ProfitFactorTableView : UserControl
{
    private ProfitFactorTableViewModel ProfitFactorTableViewModel
        => (ProfitFactorTableViewModel?)DataContext;


    protected override void OnDataContextChanged(EventArgs e)
    {
        if (ProfitFactorTableViewModel is null)
            return;
    }

    public ProfitFactorTableView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}