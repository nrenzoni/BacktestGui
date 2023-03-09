using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace BacktestGui.Views.Shared;

public partial class DateRangeSelectorView : UserControl
{
    public DateRangeSelectorView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}