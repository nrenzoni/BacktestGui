using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using BacktestGui.ViewModels.TabViewModels;

namespace BacktestGui.Views.TabViews;

public partial class IndividualBacktestStatViewerTabView :
    ReactiveUserControl<IndividualBacktestStatViewerTabViewModel>
{
    public IndividualBacktestStatViewerTabView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}