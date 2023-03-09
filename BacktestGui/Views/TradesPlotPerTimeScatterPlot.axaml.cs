using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using BacktestCSharpShared;
using BacktestGui.Services;
using BacktestGui.Utils;
using BacktestGui.ViewModels;
using BacktestGui.Views.Helpers;
using ReactiveUI;
using ScottPlot;
using ScottPlot.Avalonia;
using Color = System.Drawing.Color;

namespace BacktestGui.Views;

public partial class TradesPlotPerTimeScatterPlot : UserControl
{
    private TradesPlotPerTimeScatterPlotViewModel ViewModel
        => DataContext as TradesPlotPerTimeScatterPlotViewModel;

    readonly ScottPlotHelperFactory _scottPlotHelperFactory;


    AvaPlot ScotPlot => this.FindControl<AvaPlot>("AvaPlot1");

    public TradesPlotPerTimeScatterPlot()
    {
        _scottPlotHelperFactory = Bootstrapper.GetService<ScottPlotHelperFactory>();

        InitializeComponent();
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        ViewModel
            .WhenAnyValue(x => x.Trades)
            .Throttle(TimeSpan.FromMilliseconds(200))
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(ReloadChart);

        ScotPlot.Plot.Title("Individual Trade Pnl");
        ScotPlot.Plot.XAxis.Label("Time");
        ScotPlot.Plot.YAxis.Label("Pnl %");
    }

    private void ReloadChart(List<TradeRecord>? trades)
    {
        ScotPlot.Plot.Clear();

        if (trades is null)
            return;

        List<PnlPoint> profitLossPerTrade = new();

        foreach (var tradeRecord in trades)
        {
            profitLossPerTrade.Add(new
                (tradeRecord.CalcTradePnlPct(), tradeRecord.OpenTime));
        }

        var scottPlotHelper = trades
            .Select(x => x.OpenTime)
            .GetScotPlotHelper(_scottPlotHelperFactory);

        var (xs, ys, boundaryStartOfDayLookupKeys) =
            scottPlotHelper.ExtractXySkipNonTradeDays(profitLossPerTrade);

        var customTickFormatterSkipNonTradeDays =
            scottPlotHelper.BuildCustomTickFormatterSkipNonTradeDays(boundaryStartOfDayLookupKeys);

        foreach (var (boundary4AmNy, _) in boundaryStartOfDayLookupKeys.Skip(1))
            ScotPlot.Plot.AddVerticalLine(boundary4AmNy, Color.DarkGray);

        ScotPlot.Plot.AddScatterPoints(xs, ys);
        ScotPlot.Plot.AddHorizontalLine(0, Color.LightGray, style: LineStyle.Dash);
        ScotPlot.Plot.XAxis.Dims.SetBoundsOuter(xs.First(), xs.Last());
        ScotPlot.Plot.YAxis.Dims.SetBoundsOuter(ys.Min(), ys.Max());
        ScotPlot.Plot.XAxis.TickLabelFormat(customTickFormatterSkipNonTradeDays);

        ScotPlot.Plot.Render();
        // ScotPlot.Plot.XAxis.Dims.SetBoundsOuter(-0.001, Convert.ToDouble(maxX) * 1.1);
        // ScotPlot.Plot.YAxis.Dims.SetBoundsOuter(-0.001, Convert.ToDouble(maxY) * 1.1);
        ScotPlot.Plot.AxisAuto();
    }


    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}