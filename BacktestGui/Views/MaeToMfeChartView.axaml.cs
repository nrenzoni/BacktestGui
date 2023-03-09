using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using BacktestGui.ViewModels;
using CustomShared;
using ReactiveUI;
using ScottPlot.Avalonia;
using Color = System.Drawing.Color;

namespace BacktestGui.Views;

public partial class MaeToMfeChartView : UserControl
{
    private MaeToMfeChartViewModel ViewModel => DataContext as MaeToMfeChartViewModel;
    
    AvaPlot ScotPlot => this.FindControl<AvaPlot>("AvaPlot1");

    private readonly Color _slope1Line = Color.FromArgb(255 / 2, Color.Gray);
    private readonly Color _greenPoint = Color.FromArgb((int)(255 * 0.8), Color.Green);
    private readonly Color _redPoint = Color.FromArgb((int)(255 * 0.8), Color.DarkRed);

    protected override void OnDataContextChanged(EventArgs e)
    {
        ViewModel
            .WhenAnyValue(x => x.MaeMfePoints)
            .Throttle(TimeSpan.FromMilliseconds(200))
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(ReloadChart);

        ScotPlot.Plot.Title("Mae vs Mfe");
        ScotPlot.Plot.XAxis.Label("Mae");
        ScotPlot.Plot.YAxis.Label("Mfe");
    }

    public MaeToMfeChartView()
    {
        /*this.WhenActivated(disposable =>
        {
            ViewModel
                .WhenAnyValue(x => x.MaeMfePoints)
                .Throttle(TimeSpan.FromMilliseconds(200))
                .WhereNotNull()
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(ReloadChart)
                .DisposeWith(disposable);

            ScotPlot.Plot.Title("Mae vs Mfe");
            var transparentGray = Color.FromArgb(255 / 2, Color.Gray);
            ScotPlot.Plot.AddLine(1, 0, (-100, 100), transparentGray);
            ScotPlot.Plot.XAxis.Label("Mae");
            ScotPlot.Plot.YAxis.Label("Mfe");
        });*/

        InitializeComponent();
    }
    
    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void ReloadChart(IList<MaeMfeTradePoint>? maeMfePoints)
    {
        ScotPlot.Plot.Clear();

        if (maeMfePoints is null)
            return;
        
        var profitableTradesXs = new List<double>(maeMfePoints.Count);
        var profitableTradesYs = new List<double>(maeMfePoints.Count);
        var lossTradesXs = new List<double>(maeMfePoints.Count);
        var lossTradesYs = new List<double>(maeMfePoints.Count);

        double minX = double.MaxValue,
            maxX = double.MinValue,
            minY = double.MaxValue,
            maxY = double.MinValue;

        foreach (var (maeMfeTradePoint, i) in maeMfePoints.WithIndex())
        {
            var x = Convert.ToDouble(maeMfeTradePoint.MaeValue);
            var y = Convert.ToDouble(maeMfeTradePoint.MfeValue);

            if (maeMfeTradePoint.profitableTrade)
            {
                profitableTradesXs.Add(x);
                profitableTradesYs.Add(y);
            }
            else
            {
                lossTradesXs.Add(x);
                lossTradesYs.Add(y);
            }

            minX = Math.Min(minX, x);
            maxX = Math.Max(maxX, x);
            minY = Math.Min(minY, y);
            maxY = Math.Max(maxY, y);
        }

        ScotPlot.Plot.Title($"Mae vs Mfe ({maeMfePoints.Count} points)");
        ScotPlot.Plot.AddLine(1, 0, (-100, 100), _slope1Line);

        ScotPlot.Plot.AddScatterPoints(
            profitableTradesXs.ToArray(),
            profitableTradesYs.ToArray(),
            _greenPoint);
        ScotPlot.Plot.AddScatterPoints(
            lossTradesXs.ToArray(),
            lossTradesYs.ToArray(),
            _redPoint);

        // use max of X and Y for both axis, to keep square
        // var forArrUpperBounds = Math.Max(Convert.ToDouble(maxX), Convert.ToDouble(maxY));

        ScotPlot.Plot.Render();
        ScotPlot.Plot.XAxis.Dims.SetBoundsOuter(-0.001, Convert.ToDouble(maxX) * 1.1);
        ScotPlot.Plot.YAxis.Dims.SetBoundsOuter(-0.01, Convert.ToDouble(maxY) * 1.1);
        ScotPlot.Plot.AxisAuto();
    }
}