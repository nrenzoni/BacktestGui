using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using BacktestCSharpShared;
using BacktestGui.Models;
using ReactiveUI;

namespace BacktestGui.ViewModels;

public class TradesViewModel : ReactiveObject
{
    public TradesViewModel(IBacktestSelectorViewModel backtestSelectorViewModel)
    {
        _trades
            = backtestSelectorViewModel
                .WhenAnyValue(x => x.SelectedBacktest)
                .Throttle(TimeSpan.FromMilliseconds(200))
                .Select(x =>
                    x?.TradeRecords?.Select(ForView))
                .ObserveOn(RxApp.MainThreadScheduler)
                .ToProperty(this, x => x.Trades);
    }

    private readonly ObservableAsPropertyHelper<IEnumerable<TradeRecordForView>?> _trades;

    public IEnumerable<TradeRecordForView>? Trades => _trades.Value;

    private TradeRecordForView ForView(TradeRecord tradeRecord, int number)
    {
        var pnl = tradeRecord.CalcTradePnl();

        var tradeRecordForView
            = new TradeRecordForView(
                number,
                tradeRecord.Symbol,
                tradeRecord.OpenTime,
                tradeRecord.CloseTime,
                tradeRecord.IsLongTrade,
                tradeRecord.AvgEntryPrice,
                tradeRecord.AvgExitPrice,
                tradeRecord.CumEntryShares,
                tradeRecord.CumExitShares,
                tradeRecord.OpenPrice,
                tradeRecord.ClosePrice,
                tradeRecord.MinPrice,
                tradeRecord.MaxPrice,
                pnl);

        return tradeRecordForView;
    }
}