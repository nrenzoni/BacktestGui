using System.Collections.Generic;
using BacktestCSharpShared;
using BacktestGui.Models;

namespace BacktestGui.Utils;

public class BacktestGroupingHelper
{
    public List<BacktestGroupingParamRecord> GetBacktestGroupingParamsWithMoreThan1UniqueVal(
        List<BacktestRunEntryMongoWithTrades> backtests)
    {
        var backtestsGroupingUniqueValCountDict =
            GetBacktestGroupingParamStructsToUniqueValCountDict(backtests);

        var paramStructs = new List<BacktestGroupingParamRecord>();

        foreach (var (backtestGroupingParamRecord, uniqueValCount) in backtestsGroupingUniqueValCountDict)
        {
            if (uniqueValCount <= 1)
                continue;

            paramStructs.Add(backtestGroupingParamRecord);
        }

        return paramStructs;
    }

    public Dictionary<BacktestGroupingParamRecord, uint> GetBacktestGroupingParamStructsToUniqueValCountDict(
        List<BacktestRunEntryMongoWithTrades> backtests)
    {
        var mapToVals = new Dictionary<BacktestGroupingParamRecord, HashSet<object>>();

        foreach (var backtest in backtests)
        {
            foreach (var (strategyName, val) in backtest.StrategyParameterDictionaries)
            {
                var paramsDict = (Dictionary<string, object>)val;

                foreach (var (paramName, paramVal) in paramsDict)
                {
                    var backtestGroupingParamRecord = new BacktestGroupingParamRecord(strategyName, paramName);

                    var found = mapToVals.TryGetValue(backtestGroupingParamRecord, out var paramSet);

                    if (!found)
                    {
                        paramSet = new();
                        mapToVals[backtestGroupingParamRecord] = paramSet;
                    }

                    paramSet.Add(paramVal);
                }
            }
        }

        var mapToCount = new Dictionary<BacktestGroupingParamRecord, uint>();

        foreach (var (grouping, paramSet) in mapToVals)
        {
            mapToCount[grouping] = (uint)paramSet.Count;
        }

        return mapToCount;
    }

    public BacktestGroupingParamViewRecord GetSelectableParamsForBacktestGrouping(
        uint groupingNumber,
        List<BacktestRunEntryMongoWithTrades> backtests)
    {
        var singleStrat = true;

        foreach (var backtest in backtests)
        {
            if (backtest.StrategyParameterDictionaries.Count > 1)
            {
                singleStrat = false;
                break;
            }
        }

        var backtestGroupingParamsWithMoreThan1UniqueVal =
            GetBacktestGroupingParamsWithMoreThan1UniqueVal(backtests);

        backtestGroupingParamsWithMoreThan1UniqueVal.Sort();

        return new BacktestGroupingParamViewRecord(singleStrat, backtestGroupingParamsWithMoreThan1UniqueVal);
    }
}