using System.Collections.Generic;

namespace BacktestGui.Models;

// metrics are per single backtest (bot aggregated backtests)
public record ParamHolderRecord(
    double ParamValue,
    Dictionary<YAxisPlotType, decimal> ValueDictionary);