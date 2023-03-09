using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace BacktestGui.Views;

public partial class BacktestGroupSelectorView : UserControl
{
    public BacktestGroupSelectorView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}