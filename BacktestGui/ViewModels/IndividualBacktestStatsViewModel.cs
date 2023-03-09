using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using BacktestCSharpShared;
using BacktestGui.Configurations;
using BacktestGui.Models;
using BacktestGui.Services;
using CustomShared;
using NodaTime;
using NodaTime.Calendars;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace BacktestGui.ViewModels;

public record BacktestRowStats(
    string GroupId,
    uint NumTrades,
    uint NumLongTrades,
    uint NumShortTrades,
    uint NumWins,
    uint NumLosses,
    uint NumSymbolsTraded,
    DecimalWithInf WinToLossRatio,
    decimal NetPnl,
    decimal CumProfit,
    decimal CumLoss,
    DecimalWithInf CumProfitToCumLossRatio);

public class IndividualBacktestStatsViewModel : ReactiveObject
{
    private readonly IBacktestSelectorViewModel _backtestSelectorViewModel;

    private readonly ObservableAsPropertyHelper<BacktestVmModel?>
        _backtest;

    public BacktestVmModel? Backtest
        => _backtest?.Value;

    private readonly ObservableAsPropertyHelper<BacktestRowStats?> _allTradesRowStats;

    private BacktestRowStats? AllTradesRowStats => _allTradesRowStats.Value;

    public IndividualBacktestStatsViewModel(
        IBacktestSelectorViewModel backtestSelectorViewModel,
        Globals globals)
    {
        _backtestSelectorViewModel = backtestSelectorViewModel;

        var pub =
            _backtestSelectorViewModel.WhenAnyValue(
                    x => x.SelectedBacktest)
                .Throttle(globals.ThrottleTime)
                .Publish();

        _backtest
            = pub
                .ObserveOn(RxApp.MainThreadScheduler)
                .ToProperty(this, nameof(Backtest));

        _allTradesRowStats = pub
            .ObserveOn(RxApp.MainThreadScheduler)
            .Where(x => x?.TradeRecords is not null)
            .Select(CalcAllTradesBacktestStats)
            .ToProperty(this, nameof(AllTradesRowStats));

        _backtestStats
            = pub
                .CombineLatest(this.WhenAnyValue(x => x.RowGrouping,
                    x => x.AllTradesRowStats))
                .DistinctUntilChanged()
                .Throttle(globals.ThrottleTime)
                .Where(x => x.Second.Item2 is not null)
                .ObserveOn(RxApp.TaskpoolScheduler)
                .Select(x => (x.First, x.Second.Item1, x.Second.Item2))
                .Select(UpdateBacktestStats)
                .ObserveOn(RxApp.MainThreadScheduler)
                .ToProperty(this, nameof(BacktestStats));

        pub.Connect();
    }

    private readonly ObservableAsPropertyHelper<IEnumerable<BacktestRowStats>?> _backtestStats;

    public IEnumerable<BacktestRowStats>? BacktestStats => _backtestStats.Value;

    BacktestRowStats? CalcAllTradesBacktestStats(BacktestVmModel? backtestVmModel)
    {
        if (backtestVmModel?.TradeRecords is null)
            return null;
        
        return CreateBacktestRowStats(
            backtestVmModel.TradeRecords, "AllTrades");
    }

    IEnumerable<BacktestRowStats>? UpdateBacktestStats(
        (BacktestVmModel? First, RowGroupingType, BacktestRowStats) inputTuple)
    {
        var (backtest, rowGroupingType, allTradesStats) = inputTuple;

        if (backtest is null)
            return default;
        
        var groupedTrades
            = GroupTrades(backtest?.TradeRecords,
                rowGroupingType);

        List<BacktestRowStats> statsPerGrouping = new() { allTradesStats };
        statsPerGrouping.AddRange(groupedTrades.Select(CreateBacktestStats));
        
        return statsPerGrouping;
    }

    private BacktestRowStats CreateBacktestStats(
        IStatTradeGrouping statTradeGrouping)
    {
        return CreateBacktestRowStats(
            statTradeGrouping.Trades,
            statTradeGrouping.GroupingKeyStr);
    }

    private BacktestRowStats CreateBacktestRowStats(
        List<TradeRecord> trades,
        string groupId)
    {
        var totTrades = (uint)trades.Count;
        var numLongTrades = CalcNumLongTrades(trades);
        var numShortTrades = totTrades - numLongTrades;
        var numWinTrades = CalcNumWinTrades(trades);
        var numLosses = totTrades - numWinTrades;
        DecimalWithInf winToLossesRatio = DecimalWithInf.FromDivision(
            new decimal(numWinTrades),
            new decimal(numLosses));
        var symbolsTraded = CountSymbolsTraded(trades);
        var (cumProfit, cumLoss) = CalcCumProfitAndLoss(trades);
        var netPnl = cumProfit - cumLoss;
        var cumProfitToCumLossRatio =
            DecimalWithInf.FromDivision(cumProfit, cumLoss);

        var backtestRowStats = new BacktestRowStats(
            groupId,
            totTrades,
            numLongTrades,
            numShortTrades,
            numWinTrades,
            numLosses,
            symbolsTraded,
            winToLossesRatio,
            netPnl,
            cumProfit,
            cumLoss,
            cumProfitToCumLossRatio
        );

        return backtestRowStats;
    }

    private (decimal cumProfit, decimal cumLoss) CalcCumProfitAndLoss(
        List<TradeRecord> trades)
    {
        decimal cumProfit = 0M, cumLoss = 0M;

        foreach (var trade in trades)
        {
            var currTradeVal =
                trade.AvgExitPrice * Math.Abs(trade.CumExitShares) -
                trade.AvgEntryPrice * trade.CumEntryShares;

            if (!trade.IsLongTrade)
                currTradeVal *= -1;

            if (currTradeVal > 0)
                cumProfit += currTradeVal;
            else
                cumLoss -= currTradeVal;
        }

        return (cumProfit, cumLoss);
    }

    private uint CountSymbolsTraded(List<TradeRecord> trades)
    {
        return (uint)trades.Select(x => x.Symbol)
            .ToHashSet()
            .Count;
    }

    private uint CalcNumLongTrades(List<TradeRecord> trades)
        => (uint)trades.Count(x => x.IsLongTrade);

    private uint CalcNumWinTrades(List<TradeRecord> trades)
    {
        return (uint)trades.Count(x =>
            (x.IsLongTrade && x.AvgExitPrice > x.AvgEntryPrice) ||
            (!x.IsLongTrade && x.AvgEntryPrice > x.AvgExitPrice));
    }

    private List<IStatTradeGrouping> GroupTrades(
        List<TradeRecord>? trades,
        RowGroupingType rowGroupingType)
    {
        if (trades is null)
            return default;

        switch (rowGroupingType)
        {
            case RowGroupingType.Day:
                return GroupTradesByDay(trades);
            case RowGroupingType.Week:
                return GroupTradesByWeek(trades);
            case RowGroupingType.Month:
                return GroupTradesByMonth(trades);
        }

        throw new ArgumentOutOfRangeException(nameof(rowGroupingType), rowGroupingType, null);
    }

    private List<IStatTradeGrouping> GroupTradesByMonth(List<TradeRecord> trades)
    {
        DefaultableDictionary<YearMonth, StatTradeGroupingByMonth> tradesByMonth
            = new(() => new());

        foreach (var tradeRecord in trades)
        {
            var month = tradeRecord.OpenTime.GetNyDate().ToYearMonth();
            tradesByMonth[month]
                .Trades
                .Add(tradeRecord);
        }

        foreach (var (yearMonth, statTradeGroupingByDay) in tradesByMonth)
        {
            statTradeGroupingByDay.GroupingKey = yearMonth;
        }

        return tradesByMonth.Values.Cast<IStatTradeGrouping>().ToList();
    }

    private List<IStatTradeGrouping> GroupTradesByWeek(List<TradeRecord> trades)
    {
        DefaultableDictionary<WeekYear, StatTradeGroupingByWeek> tradesByWeek
            = new(() => new());

        foreach (var tradeRecord in trades)
        {
            var weekYear = tradeRecord.OpenTime.GetNyDate().ToWeekYear();
            tradesByWeek[weekYear]
                .Trades
                .Add(tradeRecord);
        }

        foreach (var (weekYear, statTradeGroupingByDay) in tradesByWeek)
        {
            statTradeGroupingByDay.GroupingKey = weekYear;
        }

        return tradesByWeek.Values.Cast<IStatTradeGrouping>().ToList();
    }

    private List<IStatTradeGrouping> GroupTradesByDay(
        List<TradeRecord> trades)
    {
        DefaultableDictionary<LocalDate, StatTradeGroupingByDay> tradesByDay
            = new(() => new());

        foreach (var tradeRecord in trades)
        {
            var nyDate = tradeRecord.OpenTime.GetNyDate();
            tradesByDay[nyDate]
                .Trades
                .Add(tradeRecord);
        }

        foreach (var (date, statTradeGroupingByDay) in tradesByDay)
        {
            statTradeGroupingByDay.GroupingKey = date;
        }

        return tradesByDay.Values.Cast<IStatTradeGrouping>().ToList();
    }

    [Reactive] public RowGroupingType RowGrouping { get; set; }

    public IEnumerable<RowGroupingType> RowGroupingTypeEnums
        => Enum.GetValues(typeof(RowGroupingType))
            .Cast<RowGroupingType>();

    public enum RowGroupingType
    {
        Day,
        Week,
        Month
    }

    [Reactive] public bool ShowLongStats { get; set; } = true;

    [Reactive] public bool ShowShortStats { get; set; } = true;
}

public interface IStatTradeGrouping
{
    public string GroupingKeyStr { get; }

    public List<TradeRecord> Trades { get; }
}

public interface IStatTradeGrouping<TGroupKey> : IStatTradeGrouping
{
    public TGroupKey GroupingKey { get; set; }
}

public abstract class StatTradeGroupingBase<TGroupKey> : IStatTradeGrouping<TGroupKey>
{
    public abstract string GroupingKeyStr { get; }

    public List<TradeRecord> Trades { get; } = new();

    public abstract TGroupKey GroupingKey { get; set; }
}

public class StatTradeGroupingByDay : StatTradeGroupingBase<LocalDate>
{
    public override LocalDate GroupingKey { get; set; }

    public override string GroupingKeyStr => GroupingKey.ToYYYYMMDD();
}

public class WeekYear : IComparable<WeekYear>, IEquatable<WeekYear>
{
    public WeekYear(uint year, uint week)
    {
        Year = year;
        Week = week;
    }

    public uint Week { get; }

    public uint Year { get; }

    public int CompareTo(WeekYear? other)
    {
        if (ReferenceEquals(this, other)) return 0;
        if (ReferenceEquals(null, other)) return 1;
        var yearComparison = Year.CompareTo(other.Year);
        if (yearComparison != 0) return yearComparison;
        return Week.CompareTo(other.Week);
    }

    public bool Equals(WeekYear? other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return Week == other.Week && Year == other.Year;
    }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != this.GetType()) return false;
        return Equals((WeekYear)obj);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Week, Year);
    }
}

public static class WeekYearExtMethods
{
    public static WeekYear ToWeekYear(this LocalDate date)
    {
        var week = WeekYearRules.Iso.GetWeekOfWeekYear(date);
        // can be different year than LocalDate's year
        var year = WeekYearRules.Iso.GetWeekYear(date);


        return new WeekYear((uint)year, (uint)week);
    }
}

public class StatTradeGroupingByWeek : StatTradeGroupingBase<WeekYear>
{
    public override WeekYear GroupingKey { get; set; }

    public override string GroupingKeyStr => $"{GroupingKey.Year}-{GroupingKey.Week}";
}

public class StatTradeGroupingByMonth : StatTradeGroupingBase<YearMonth>
{
    public override YearMonth GroupingKey { get; set; }

    public override string GroupingKeyStr => $"{GroupingKey.Year}-{GroupingKey.Month}";
}