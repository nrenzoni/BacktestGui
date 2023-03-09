using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using BacktestGui.Configurations;
using BacktestGui.Models;
using BacktestGui.Services;
using NodaTime;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace BacktestGui.ViewModels;

public record MaeMfeTradePoint(
    decimal MaeValue,
    decimal MfeValue,
    bool profitableTrade,
    Instant Time);

public class MaeToMfeChartViewModel : ReactiveObject
{
    public MaeToMfeChartViewModel(IBacktestSelectorViewModel backtestSelectorViewModel, Globals globals)
    {
        backtestSelectorViewModel
            .WhenAnyValue(x => x.SelectedBacktest)
            .Throttle(globals.ThrottleTime)
            .Select(GetAsMaeMfePoints)
            .BindTo(this, x => x.MaeMfePoints);
    }

    private IList<MaeMfeTradePoint>? GetAsMaeMfePoints(BacktestVmModel? backtestVmModel)
    {
        if (backtestVmModel is null)
            return null;
        
        var trades = backtestVmModel.TradeRecords;

        var maeMfeTradePoints = new List<MaeMfeTradePoint>();

        if (trades is null)
            return maeMfeTradePoints;


        foreach (var tradeRecord in trades)
        {
            var entryPosAvg
                = tradeRecord.CumEntryShares * tradeRecord.AvgEntryPrice;

            var allValsArr =
                new[]
                {
                    tradeRecord.AvgEntryPrice, tradeRecord.MinPrice, tradeRecord.MaxPrice, tradeRecord.AvgExitPrice
                };

            var minPrice = allValsArr.Min();
            var maxPrice = allValsArr.Max();

            var exitSharesAbs = Math.Abs(tradeRecord.CumExitShares);

            var mae = 1 - (exitSharesAbs * minPrice)
                / entryPosAvg;
            var mfe = (exitSharesAbs * maxPrice)
                / entryPosAvg - 1M;

            if (!tradeRecord.IsLongTrade)
                (mae, mfe) = (mfe, mae);

            var netProfit = tradeRecord.IsLongTrade
                ? tradeRecord.ClosePrice > tradeRecord.OpenPrice
                : tradeRecord.ClosePrice < tradeRecord.OpenPrice;

                maeMfeTradePoints.Add(
                new(mae, mfe, netProfit, tradeRecord.OpenTime));
        }

        return maeMfeTradePoints;
    }

    [Reactive] public IList<MaeMfeTradePoint>? MaeMfePoints { get; set; }
}