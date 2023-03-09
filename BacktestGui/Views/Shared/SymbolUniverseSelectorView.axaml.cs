using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace BacktestGui.Views.Shared;

public partial class SymbolUniverseSelectorView : UserControl
{
    public SymbolUniverseSelectorView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}