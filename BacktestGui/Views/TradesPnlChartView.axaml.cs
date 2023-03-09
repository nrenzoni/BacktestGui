using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reactive.Linq;
using Avalonia.Controls;
using Avalonia.Controls.Mixins;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using BacktestGui.Configurations;
using BacktestGui.Services;
using BacktestGui.Utils;
using BacktestGui.ViewModels;
using BacktestGui.Views.Helpers;
using CustomShared;
using NodaTime;
using ReactiveUI;
using ScottPlot.Avalonia;

namespace BacktestGui.Views;

public partial class TradesPnlChartView :
    ReactiveUserControl<TradesPnlChartViewModel>
{
    public TradesPnlChartView()
    {
        _scottPlotHelperFactory = Bootstrapper.GetService<ScottPlotHelperFactory>();
        
        var globals = Bootstrapper.GetService<Globals>();

        InitializeComponent();

        this.WhenActivated(disposable =>
        {
            ViewModel.WhenAnyValue(
                    x => x.PnlPoints)
                .Throttle(globals.ThrottleTime)
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(UpdateChartData)
                .DisposeWith(disposable);
        });
    }

    readonly ScottPlotHelperFactory _scottPlotHelperFactory;

    private static double DateToPlottableFormat(Instant time)
    {
        return time.InZone(DateUtils.NyDateTz).ToDateTimeUnspecified().ToOADate();
    }

    private (double[], double[]) ExtractXyFromPnlPoints(List<PnlPoint> pnlPoints)
    {
        List<double> xs = new(), ys = new();

        pnlPoints.ForEach(x =>
        {
            xs.Add(DateToPlottableFormat(x.Time));
            ys.Add(Convert.ToDouble(x.Value));
        });

        return (xs.ToArray(), ys.ToArray());
    }

    private void UpdateChartData(List<PnlPoint>? pnlPoints)
    {
        ScotPlot.Plot.Clear();

        ScotPlot.Plot.Title($"Pnl Chart");

        if (pnlPoints is null)
            return;

        var scottPlotHelper = pnlPoints
            .Select(x => x.Time)
            .GetScotPlotHelper(_scottPlotHelperFactory);

        // var (xs, ys) = ExtractXyFromPnlPoints(pnlPoints);
        var (xs, ys, boundaryStartOfDayLookupKeys)
            = scottPlotHelper.ExtractXySkipNonTradeDays(pnlPoints);

        ScotPlot.Plot.AddScatterLines(xs, ys);
        foreach (var (boundary4AmNy, _) in boundaryStartOfDayLookupKeys.Skip(1))
            ScotPlot.Plot.AddVerticalLine(boundary4AmNy, Color.DarkGray);

        var customTickFormatterSkipNonTradeDays =
            scottPlotHelper.BuildCustomTickFormatterSkipNonTradeDays(boundaryStartOfDayLookupKeys);
        ScotPlot.Plot.XAxis.Dims.SetBoundsOuter(xs.First(), xs.Last());
        ScotPlot.Plot.YAxis.Dims.SetBoundsOuter(ys.Min(), ys.Max());
        ScotPlot.Plot.XAxis.TickLabelFormat(customTickFormatterSkipNonTradeDays);
        // ScotPlot.Plot.XAxis.DateTimeFormat(true);

        ScotPlot.Render();
        ScotPlot.Plot.AxisAuto();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    AvaPlot ScotPlot => this.FindControl<AvaPlot>("Plot1");
}