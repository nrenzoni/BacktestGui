using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace BacktestGui.Views.Shared;

public partial class SamplingFreqSelectorView : UserControl
{
    public SamplingFreqSelectorView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}