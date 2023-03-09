using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace BacktestGui.Views.TabViews;

public partial class IndicatorResearchTabView : UserControl
{
    public IndicatorResearchTabView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}