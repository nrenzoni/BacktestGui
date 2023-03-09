using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using BacktestGui.Models;
using CustomShared;
using NodaTime;
using ReactiveUI;

namespace BacktestGui.ViewModels;

public record PnlPoint(
    decimal Value,
    Instant Time);

public class TradesPnlChartViewModel : ReactiveObject
{
    private readonly ObservableAsPropertyHelper<List<PnlPoint>?> _pnlPoints;
    public List<PnlPoint>? PnlPoints => _pnlPoints.Value;

    public TradesPnlChartViewModel(IBacktestSelectorViewModel backtestSelectorViewModel)
    {
        _pnlPoints
            = backtestSelectorViewModel.WhenAnyValue(x => x.SelectedBacktest)
                .Throttle(TimeSpan.FromMilliseconds(200))
                .Select(UpdatePnlPoints)
                .ToProperty(this, nameof(PnlPoints));
    }

    private List<PnlPoint>? UpdatePnlPoints(BacktestVmModel? backtest)
    {
        if (backtest is null)
            return null;
        
        var nonNormalizedPnlPoints = CalculatePnlPoints(backtest);
        if (!backtest.ParallelRun)
            return nonNormalizedPnlPoints;

        var normalizedPnlPoints
            = NormalizePnlForParallelMultiDayBacktest(
                nonNormalizedPnlPoints);
        return normalizedPnlPoints;
    }

    private List<PnlPoint> CalculatePnlPoints(BacktestVmModel backtest)
    {
        var orders = backtest.OrderFillLogEntries;

        var initCapital = backtest.InitialCapital;
        var simStartTime = backtest.SimulatedStartTime;
        var ordersCount = orders.Count;

        var pnlPoints
            = new List<PnlPoint>(ordersCount + 2);

        pnlPoints.Add(
            new(initCapital, simStartTime));

        var cumCapital = initCapital;
        var currTime = simStartTime;

        foreach (var order in orders)
        {
            cumCapital = order.FilledExposedTotDollarVal
                         + order.RemainingAccountCapital
                         + order.TotalOtherOpenPositionsDollarValue;
            currTime = order.FilledTime;

            pnlPoints.Add(
                new(cumCapital, currTime));
        }

        Instant simEndTime;
        if (backtest.SimulatedEndTime != null)
        {
            simEndTime = backtest.SimulatedEndTime;
        }
        else
            simEndTime =
                (currTime.GetNyDate() + new LocalTime(20, 0))
                .InZoneStrictly(DateUtils.NyDateTz)
                .ToInstant();

        pnlPoints.Add(
            new(cumCapital, simEndTime));

        return pnlPoints;
    }

    private List<PnlPoint> NormalizePnlForParallelMultiDayBacktest(
        List<PnlPoint> pnlPoints)
    {
        if (pnlPoints.Count <= 1)
            return pnlPoints;

        var normalizedPnlPoints = new List<PnlPoint>(pnlPoints.Count);

        normalizedPnlPoints.Add(
            pnlPoints.First());

        using var enumerator = pnlPoints.GetEnumerator();

        enumerator.MoveNext();

        var lastDateProcessed = enumerator.Current.Time.GetNyDate();
        var lastFilledPrice = enumerator.Current.Value;

        decimal normalizationFactor = 1M;

        while (enumerator.MoveNext())
        {
            var currOrder = enumerator.Current;

            var currFillDate = currOrder.Time.GetNyDate();
            var currFillPrice = currOrder.Value;

            if (currFillDate != lastDateProcessed)
            {
                normalizationFactor = currFillPrice / lastFilledPrice;
                lastDateProcessed = currFillDate;
            }

            lastFilledPrice = currFillPrice;

            var normalizedVal = currFillPrice / normalizationFactor;

            normalizedPnlPoints.Add(
                new(normalizedVal,
                    currOrder.Time));
        }

        return normalizedPnlPoints;
    }
}