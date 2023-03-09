using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BacktestCSharpShared;
using CustomShared;
using NodaTime;

namespace BacktestGui.Services;

public class BacktestService
{
    private readonly MongoBacktestsRepo _backtestsRepo;
    readonly TimeSpan _delayBetweenRetries = TimeSpan.FromSeconds(1);

    public BacktestService(MongoBacktestsRepo backtestsRepo)
    {
        _backtestsRepo = backtestsRepo;
    }

    public List<BacktestRunEntryMongoWithTrades> GetBacktestEntries()
    {
        return ExceptionFuncs.RunWithRetries(
            () => _backtestsRepo.GetBacktestEntries(),
            nameof(_backtestsRepo.GetBacktestEntries),
            null,
            _delayBetweenRetries);
    }

    public Dictionary<LocalDate, List<TradeRecord>> GroupTradesByDay(List<TradeRecord> trades)
    {
        var tradesByDay = new Dictionary<LocalDate, List<TradeRecord>>();

        foreach (var trade in trades)
        {
            var nyDate = trade.OpenTime.GetNyDate();

            var found = tradesByDay.TryGetValue(
                nyDate,
                out var currentDayList);

            if (!found)
            {
                currentDayList = new();
                tradesByDay[nyDate] = currentDayList;
            }

            currentDayList.Add(trade);
        }

        return tradesByDay;
    }

    public Dictionary<int, List<BacktestRunEntryMongoWithTrades>> GroupBacktestsByGroupingNumber(
        List<BacktestRunEntryMongoWithTrades> backtests)
    {
        Dictionary<int, List<BacktestRunEntryMongoWithTrades>> backtestsGrouped = new();

        foreach (var backtest in backtests)
        {
            if (backtest.GroupingNumber is null)
                continue;

            var backtestGroupingNumber = backtest.GroupingNumber.Value;

            var listFound = backtestsGrouped.TryGetValue(backtestGroupingNumber, out var currBacktestList);

            if (!listFound)
            {
                currBacktestList = new();
                backtestsGrouped[backtestGroupingNumber] = currBacktestList;
            }

            currBacktestList.Add(backtest);
        }

        return backtestsGrouped;
    }
}