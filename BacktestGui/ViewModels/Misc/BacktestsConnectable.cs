using System;
using System.Collections.Generic;
using BacktestCSharpShared;
using DynamicData;

namespace BacktestGui.ViewModels.Misc;

public class BacktestsConnectable
{
    private readonly SourceList<BacktestRunEntryMongoWithTrades> _backtests = new();

    public void SetNewBacktests(IEnumerable<BacktestRunEntryMongoWithTrades> backtests)
    {
        _backtests.Edit(list =>
        {
            list.Clear();
            list.AddRange(backtests);
        });
    }

    public IObservable<IChangeSet<BacktestRunEntryMongoWithTrades>> Connect()
        => _backtests.Connect();
}