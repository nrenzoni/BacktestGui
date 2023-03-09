using System;
using System.Collections.Generic;
using BacktestCSharpShared;
using BacktestGui.Utils;
using BacktestGui.ViewModels;
using BacktestGui.ViewModels.Misc;
using CustomShared;
using DynamicData;
using NodaTime;
using TradeServicesSharedDotNet.Models;

namespace BacktestGui.Services;

public class Tests
{
    public static void InitBacktests()
    {
        var backtestsHolder = Bootstrapper.GetService<BacktestsConnectable>();

        var multiBacktestParametersComparisonBarChartViewModel =
            Bootstrapper.GetService<MultiBacktestParametersComparisonBarChartViewModel>();

        var backtestRunEntry1 = GenerateTestBacktest(10);
        var backtestRunEntry2 = GenerateTestBacktest(11, 35_000);
        var backtestRunEntry3 = GenerateTestBacktest(12);

        // multiBacktestParametersComparisonBarChartViewModel.ComparisonParam1 = "param1";

        var backtests = new List<BacktestRunEntryMongoWithTrades>();
        backtests.AddRange(new[]
        {
            backtestRunEntry1,
            backtestRunEntry2,
            backtestRunEntry3
        });

        backtestsHolder.SetNewBacktests(
            backtests);
    }

    public static TradeRecord BuildTestTradeRecord()
    {
        var openTime = SystemClock.Instance.GetCurrentInstant();
        var closeTime = openTime + Duration.FromSeconds(30);


        var fakeTrade = new TradeRecord(
            "ABC",
            openTime,
            closeTime,
            true,
            10,
            11,
            10,
            10,
            9.8M,
            10.1M,
            9.8M,
            10.1M
        );

        return fakeTrade;
    }

    public static BacktestRunEntryMongoWithTrades GenerateTestBacktest(
        double paramVal,
        decimal endingCapital = 31_000M)
    {
        return new BacktestRunEntryMongoWithTrades(
            DateUtils.GetCurrentInstant(),
            "123",
            new List<string>() { "strat1" },
            new Tuple<LocalDate, LocalDate>(LocalDate.MinIsoValue, LocalDate.MinIsoValue),
            30_000M,
            endingCapital,
            123,
            new Dictionary<LocalDate, List<string>>(),
            "nick",
            DateUtils.GetCurrentInstant(),
            DateUtils.GetCurrentInstant(),
            false,
            new Dictionary<string, object>
            {
                { "param1", paramVal }
            },
            new Dictionary<string, object>(),
            new List<OrderFillLogEntry>(),
            new List<OrderCreationEntry>(),
            new List<TradeRecord>()
            {
                new[]
                {
                    BuildTestTradeRecord()
                }
            });
    }
}