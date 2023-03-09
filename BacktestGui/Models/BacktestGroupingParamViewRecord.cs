using System.Collections.Generic;

namespace BacktestGui.Models;

public record BacktestGroupingParamViewRecord(
    bool SingleStrategy,
    List<BacktestGroupingParamRecord> BacktestGroupingParamRecords)
{
}