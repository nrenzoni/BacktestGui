using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using BacktestGui.ViewModels;
using ReactiveUI;

namespace BacktestGui.Views;

public partial class BacktestSelectorView : ReactiveUserControl<BacktestSelectorViewModel>
{
    public BacktestSelectorView()
    {
        InitializeComponent();

        this.WhenActivated(disposable => { });
    }

    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);
}