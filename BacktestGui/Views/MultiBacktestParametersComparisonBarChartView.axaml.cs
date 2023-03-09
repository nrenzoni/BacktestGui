using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Reactive.Linq;
using Avalonia.Controls;
using Avalonia.Controls.Mixins;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using BacktestGui.Models;
using BacktestGui.Services;
using BacktestGui.Utils;
using BacktestGui.ViewModels;
using CustomShared;
using ReactiveUI;
using ScottPlot.Avalonia;
using ScottPlot.Plottable;

namespace BacktestGui.Views;

public partial class MultiBacktestParametersComparisonBarChartView :
    ReactiveUserControl<MultiBacktestParametersComparisonBarChartViewModel>
{
    AvaPlot ScotPlot => this.FindControl<AvaPlot>("AvaPlot1");

    private BarPlot? _barPlot;

    public MultiBacktestParametersComparisonBarChartView()
    {
        this.WhenActivated(disposable =>
        {
            ViewModel.WhenAnyValue(
                    x => x.ParamHolderRecords,
                    x => x.SelectedComparisonParam1,
                    x => x.YAxisPlotType)
                .Where(x => x.NoNullMembers() && x.Item1?.Count != 0)
                .Throttle(TimeSpan.FromSeconds(0.2))
                .ObserveOn(RxApp.MainThreadScheduler)
                .Select(x =>
                    (GetAsValsAndPos(x.Item1!, x.Item3), x.Item2, x.Item3))
                .Subscribe(x =>
                    UpdateChartData(x.Item1, x.Item2, x.Item3))
                .DisposeWith(disposable);
        });

        InitializeComponent();

        SetPlotDataInitTest();
    }

    private (double[] vals, double[] positions) GetAsValsAndPos(
        ObservableCollection<ParamHolderRecord> paramHolderRecords,
        YAxisPlotType yAxisPlotType)
    {
        var valsAndPositions = new List<(double vals, double pos)>();

        for (var i = 0; i < paramHolderRecords.Count; i++)
        {
            var currParamHolderRecord = paramHolderRecords[i];

            var valFound = currParamHolderRecord.ValueDictionary.TryGetValue(
                ViewModel.YAxisPlotType,
                out var val);

            if (!valFound)
                throw new Exception(
                    $"{nameof(yAxisPlotType)} {yAxisPlotType} is not contained in {nameof(ParamHolderRecord)}.{nameof(ParamHolderRecord.ValueDictionary)}");

            var valDouble = Convert.ToDouble(val);
            var posDouble = currParamHolderRecord.ParamValue;

            valsAndPositions.Add(
                new(valDouble, posDouble));
        }

        valsAndPositions.Sort((lhs, rhs)
            => lhs.Item2.CompareTo(rhs.Item2));

        return (
            valsAndPositions.Select(x => x.vals).ToArray(),
            valsAndPositions.Select(x => x.pos).ToArray());
    }

    private void UpdateChartData(
        (double[] vals, double[] positions) valsAndPositions,
        string comparisonParam1,
        YAxisPlotType yAxisPlotType)
    {
        ScotPlot.Plot.Title($"{yAxisPlotType} of {comparisonParam1}");
        ScotPlot.Plot.XAxis.Label(comparisonParam1);
        ScotPlot.Plot.YAxis.Label(yAxisPlotType.ToString());

        SetPlotData(
            valsAndPositions.vals,
            valsAndPositions.positions);
    }

    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);

    private void SetPlotDataInitTest()
    {
        double[] vals = { 10, 12, 15, 13, 10 };
        double[] pos = Enumerable.Range(5, vals.Length).Select(Convert.ToDouble).ToArray();

        SetPlotData(vals, pos);
    }

    private void SetPlotData(double[] vals, double[] pos)
    {
        ScotPlot.Plot.Clear();

        var minCenterOfBarsDistance
            = pos
                .Pairwise()
                .Select(x => Math.Abs(x.Item2 - x.Item1))
                .Min();

        _barPlot = ScotPlot.Plot.AddBar(vals, pos);
        var barDistance = minCenterOfBarsDistance * 0.8;
        _barPlot.BarWidth = barDistance;

        ScotPlot.Plot.XTicks(
            pos,
            pos.Select(p => p.ToString(CultureInfo.InvariantCulture)).ToArray());
        var setAxisLimits = _barPlot.GetAxisLimits();
        ScottPlotUtils.SetAxisLimits(ScotPlot.Plot.YAxis.Dims, setAxisLimits.YMin, setAxisLimits.YMax);
        ScotPlot.Plot.XAxis.Dims.SetBoundsOuter(pos.Min() - barDistance, pos.Max() + barDistance);

        ScotPlot.Render();
    }
}