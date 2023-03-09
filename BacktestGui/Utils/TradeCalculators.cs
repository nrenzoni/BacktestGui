using System.Linq;
using BacktestCSharpShared;
using CustomShared;
using NodaTime;

namespace BacktestGui.Utils;

public static class TradeCalculators
{
    public static decimal CalcTradeProfit(this TradeRecord tradeRecord)
    {
        return tradeRecord.AvgExitPrice * tradeRecord.CumExitShares -
               tradeRecord.AvgEntryPrice * tradeRecord.CumEntryShares;
    }

    public static decimal CalcProfitReturnForBacktest(
        this BacktestRunEntryMongoWithTrades backtestRunEntryMongoWithTrades)
    {
        return backtestRunEntryMongoWithTrades.EndingCapital / backtestRunEntryMongoWithTrades.InitialCapital - 1;
    }

    public static double TradesPerDayAvg(this BacktestRunEntryMongoWithTrades backtestRunEntryMongoWithTrades)
    {
        if (backtestRunEntryMongoWithTrades.TradeRecords is null)
            return 0;

        var tradesPerDayDict = new DefaultableDictionary<LocalDate, int>(() => 0);

        foreach (var tradeRecord in backtestRunEntryMongoWithTrades.TradeRecords)
        {
            var date = tradeRecord.OpenTime.GetNyDate();
            tradesPerDayDict[date]++;
        }

        return tradesPerDayDict.Values.Average();
    }
}