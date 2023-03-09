using System;
using System.Collections.Generic;
using System.Linq;
using BacktestCSharpShared;
using CustomShared;
using MongoDB.Bson;
using NodaTime;
using TradeServicesSharedDotNet.Models;

namespace BacktestGui.Models;

public class BacktestVmModel
{
    public ObjectId? Id { get; init; }

    public Instant StartTime { get; init; }

    public string GitCommit { get; init; }

    public List<string> Strategies { get; init; }

    public Tuple<LocalDate, LocalDate> DateRangeIncl { get; init; }

    public decimal InitialCapital { get; init; }

    public decimal EndingCapital { get; init; }

    public Dictionary<LocalDate, List<string>> WatchedSymbolsPerDay { get; init; }

    public string? Nickname { get; init; }

    public Instant SimulatedStartTime { get; init; }

    public Instant SimulatedEndTime { get; init; }

    public bool ParallelRun { get; init; }

    // key is strategy name
    public Dictionary<string, object> StrategyParameterDictionaries { get; init; }

    public Dictionary<string, object> BacktestClassDependencies { get; init; }

    public List<OrderFillLogEntry> OrderFillLogEntries { get; init; }

    public List<OrderCreationEntry> OrderCreationEntries { get; set; }

    public List<TradeRecord>? TradeRecords { get; set; }

    public int? GroupingNumber { get; set; }

    // region computed

    public string StrategiesConcattedForView => String.Join(", ", Strategies);

    public string DateRangeStr => $"{DateRangeIncl.Item1.ToYYYYMMDD()} - {DateRangeIncl.Item2.ToYYYYMMDD()}";

    public decimal AvgWatchedSymbolsPerDay
        => (decimal)WatchedSymbolsPerDay.Values.Select(x => x.Count).Average();

    public string SimulatedStartTimeStr => SimulatedStartTime.ToStringNyTz();

    public string SimulatedEndTimeStr => SimulatedEndTime.ToStringNyTz();

    public int TotalOrderFills => OrderFillLogEntries.Count;

    public decimal Pnl => EndingCapital - InitialCapital;

    public decimal PnlPct => EndingCapital / InitialCapital - 1;

    public int? TotalRoundtripTrades => TradeRecords?.Count;

    public string IdAsStr => Id.ToString();

    // endregion
}