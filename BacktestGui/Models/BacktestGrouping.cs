using System;
using System.Collections.Generic;

namespace BacktestGui.Models;

public record BacktestGrouping(
    int GroupingNumber,
    List<string> Strategies,
    List<string> SelectableParameters)
{
    public string StrategiesConcated => String.Join(", ", Strategies);

    public string SelectableParamsConcated => String.Join(", ", SelectableParameters);
}