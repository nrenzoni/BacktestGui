using System;

namespace BacktestGui.Configurations;

public class Globals
{
    public Globals(TimeSpan throttleTime)
    {
        ThrottleTime = throttleTime;
    }

    public TimeSpan ThrottleTime { get; }
}