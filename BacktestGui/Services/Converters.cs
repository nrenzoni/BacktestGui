using BacktestCSharpShared;
using BacktestGui.Models;

namespace BacktestGui.Services;

public class Converters
{
    public BacktestVmModel ToBacktestVmModel(BacktestRunEntryMongoWithTrades backtest)
    {
        var backtestVmModel
            = new BacktestVmModel
            {
                Id = backtest.Id,
                StartTime = backtest.StartTime,
                GitCommit = backtest.GitCommit,
                Strategies = backtest.Strategies,
                DateRangeIncl = backtest.DateRangeIncl,
                InitialCapital = backtest.InitialCapital,
                EndingCapital = backtest.EndingCapital,
                WatchedSymbolsPerDay = backtest.WatchedSymbolsPerDay,
                Nickname = backtest.Nickname,
                SimulatedStartTime = backtest.SimulatedStartTime,
                SimulatedEndTime = backtest.SimulatedEndTime,
                ParallelRun = backtest.ParallelRun,
                StrategyParameterDictionaries = backtest.StrategyParameterDictionaries,
                BacktestClassDependencies = backtest.BacktestClassDependencies,
                OrderFillLogEntries = backtest.OrderFillLogEntries,
                OrderCreationEntries = backtest.OrderCreationEntries,
                TradeRecords = backtest.TradeRecords,
                GroupingNumber = backtest.GroupingNumber,
            };

        return backtestVmModel;
    }
}