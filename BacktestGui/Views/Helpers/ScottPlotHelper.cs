using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using BacktestGui.ViewModels;
using CustomShared;
using NodaTime;

namespace BacktestGui.Views.Helpers;

public class ScottPlotHelper
{
    private readonly MarketDayChecker _marketDayChecker;
    readonly LocalTime _startTimePerDay;
    readonly LocalTime _endTimePerDay;
    private double? _perDayMillis;

    public ScottPlotHelper(
        MarketDayChecker marketDayChecker,
        LocalTime? startTimePerDay = null,
        LocalTime? endTimePerDay = null)
    {
        _marketDayChecker = marketDayChecker;
        _startTimePerDay = startTimePerDay ?? new LocalTime(4, 0);
        _endTimePerDay = endTimePerDay ?? new LocalTime(20, 0);
    }

    private double CalculatePerDayMillis()
    {
        var date = LocalDate.MinIsoValue;
        var minValPerDay = (date + _startTimePerDay).InZoneStrictly(DateUtils.NyDateTz).ToInstant();
        var maxValPerDay = (date + _endTimePerDay).InZoneStrictly(DateUtils.NyDateTz).ToInstant();
        var diff = maxValPerDay - minValPerDay;
        return diff.TotalMilliseconds;
    }

    public double PerDayMillis
    {
        get
        {
            _perDayMillis ??= CalculatePerDayMillis();
            return _perDayMillis.Value;
        }
    }

    public (double[] xs, double[] ys, List<(double boundary4AmNy, LocalDate date)> boundaryStartOfDayLookupKeys)
        ExtractXySkipNonTradeDays(List<PnlPoint> pnlPoints)
    {
        var dates =
            pnlPoints
                .Select(x => x.Time.GetNyDate())
                .ToImmutableSortedSet();

        var minDate = dates.Min;
        var maxDate = dates.Max;

        var marketOpenDays =
            _marketDayChecker.GetMarketOpenDaysInRangeInclLast(
                minDate,
                maxDate).ToHashSet();

        marketOpenDays.Add(minDate);
        marketOpenDays.Add(maxDate);

        var marketOpenDaysList = marketOpenDays.ToList();

        List<double> xs = new(), ys = new();

        List<(double boundary4AmNy, LocalDate date)> boundaryStartOfDayLookupKeys = new();

        foreach (var (date, i) in marketOpenDaysList.WithIndex())
        {
            var boundary4AmNy =
                i * PerDayMillis;
            boundaryStartOfDayLookupKeys.Add((boundary4AmNy, date));
        }

        foreach (var pnlPoint in pnlPoints)
        {
            var pnlPointTime = pnlPoint.Time;
            var nyDate = pnlPointTime.GetNyDate();

            var binarySearchResult = marketOpenDaysList.CustomBinarySearch(nyDate);

            if (binarySearchResult.BinarySearchResultType is not BinarySearchResultType.Exact)
                throw new Exception(
                    $"Expecting {nameof(binarySearchResult.BinarySearchResultType)} with value of {nameof(BinarySearchResultType.Exact)} but got value {binarySearchResult.BinarySearchResultType}.");

            if (!binarySearchResult.Index.HasValue)
                throw new Exception("Bad state");

            var priorDays = binarySearchResult.Index.Value;

            var millisPriorToCurrentDay = priorDays * PerDayMillis;
            var currDayBeginBoundary = ToBeginningOfNyDayInstant(nyDate);

            if (pnlPointTime < currDayBeginBoundary)
                throw new Exception(
                    $"{nameof(Duration)} {nameof(pnlPointTime)} has time {pnlPointTime.InZone(DateUtils.NyDateTz).TimeOfDay} which is before 4am.");

            var currentDayMillis = (pnlPointTime - currDayBeginBoundary).TotalMilliseconds;

            var finalTimestamp = millisPriorToCurrentDay + currentDayMillis;

            xs.Add(finalTimestamp);
            ys.Add(Convert.ToDouble(pnlPoint.Value));
        }

        return (xs.ToArray(), ys.ToArray(), boundaryStartOfDayLookupKeys);
    }

    public Func<double, string> BuildCustomTickFormatterSkipNonTradeDays(
        List<(double boundary4AmNy, LocalDate date)> boundaryStartOfDayLookupKeys)
    {
        string CustomTickFormatterSkipNonTradeDays(double positionMillis)
        {
            var binarySearchResult =
                boundaryStartOfDayLookupKeys.CustomBinarySearch(
                    new TimeBoundaryComparer2<double, double, LocalDate>(
                        positionMillis, tuple => tuple.Item1));

            var binarySearchResultType = binarySearchResult.BinarySearchResultType;

            uint index;

            switch (binarySearchResultType)
            {
                case BinarySearchResultType.AllUnder:
                    index = (uint)(boundaryStartOfDayLookupKeys.Count - 1);
                    break;
                case BinarySearchResultType.FirstOver:
                    index = binarySearchResult.Index.Value - 1;
                    break;
                case BinarySearchResultType.Exact:
                    index = binarySearchResult.Index.Value;
                    break;
                default:
                    throw new Exception(
                        $"Unexpected {nameof(binarySearchResultType)} with value {binarySearchResultType}.");
            }

            var (_, currDay) = boundaryStartOfDayLookupKeys[(int)index];

            var currDayMillis = Duration.FromMilliseconds(positionMillis - index * PerDayMillis);
            var currPosInstant = ToBeginningOfNyDayInstant(currDay) + currDayMillis;

            var timeStrings = currPosInstant.ToStringNyTz().Split("T");
            var tempConcatListDebug = new List<string>(timeStrings);
            // tempConcatListDebug.Add($"{position}");
            return string.Join("\n", tempConcatListDebug);
        }

        return CustomTickFormatterSkipNonTradeDays;
    }

    private Instant ToBeginningOfNyDayInstant(LocalDate date)
    {
        return
            (date + _startTimePerDay)
            .InZoneStrictly(DateUtils.NyDateTz)
            .ToInstant();
    }

    private Duration ToDurationSinceEpoch(Instant instant)
    {
        return Duration.FromMilliseconds(instant.ToUnixTimeMilliseconds());
    }
}

public class TimeBoundaryComparer2<TComparison, TTuple1, TTuple2>
    : SingleValueCollectionComparer<ValueTuple<TTuple1, TTuple2>, TComparison>
    where TComparison : IComparable<TComparison>
{
    public TimeBoundaryComparer2(TComparison val, Func<(TTuple1, TTuple2), TComparison> tupleValExtractor) :
        base(val, tupleValExtractor)
    {
    }
}