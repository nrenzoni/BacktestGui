using System;
using Splat;

namespace BacktestGui.Utils;

public class Bootstrapper
{
    public static T GetService<T>()
    {
        var service = Locator.Current.GetService<T>();
        if (service is null)
        {
            throw new InvalidOperationException($"Type {typeof(T).Name} was not registered.");
        }

        return service;
    }
}